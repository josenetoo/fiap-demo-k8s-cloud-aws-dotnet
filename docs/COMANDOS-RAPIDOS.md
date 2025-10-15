# ⚡ Comandos Rápidos - Referência

> **Cheat sheet para consulta rápida durante a aula**

---

## 🔧 Setup Inicial

```bash
# Variáveis de ambiente
export AWS_PROFILE=fiapaws
export AWS_REGION=us-east-1
export CLUSTER_NAME=fiap-eks-cluster
export ECR_REPO_NAME=fiapstore-api

# Testar AWS
aws sts get-caller-identity --profile $AWS_PROFILE
```

---

## 🐳 Docker Local

```bash
# Build da imagem para AMD64 (compatível com EKS)
docker buildx build --platform linux/amd64 -t fiapstore-api:latest . --load

# Verificar imagem e arquitetura
docker images | grep fiapstore-api
docker inspect fiapstore-api:latest | grep Architecture

# Executar container localmente
docker run -d -p 8080:8080 --name fiapstore-test fiapstore-api:latest

# Testar
curl http://localhost:8080/health
curl http://localhost:8080/info | jq
curl http://localhost:8080/api/produtos | jq

# Ver logs
docker logs fiapstore-test

# Parar e remover
docker stop fiapstore-test
docker rm fiapstore-test
```

---

## 📦 ECR - Push da Imagem

```bash
# Criar repositório ECR
aws ecr create-repository --repository-name fiapstore-api --region us-east-1 --profile fiapaws

# Obter Account ID
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text --profile fiapaws)

# Definir URI (hardcoded para evitar problemas)
export ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com/fiapstore-api"
echo "ECR URI: $ECR_URI"

# Login no ECR
aws ecr get-login-password --region us-east-1 --profile fiapaws | \
    docker login --username AWS --password-stdin ${AWS_ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com

# Tag e push (imagem já buildada)
docker tag fiapstore-api:latest ${ECR_URI}:latest
docker push ${ECR_URI}:latest

# Verificar
aws ecr describe-images --repository-name fiapstore-api --region us-east-1 --profile fiapaws
```

---

## ☸️ EKS - Criar Cluster

```bash
# Criar cluster
aws eks create-cluster \
    --name $CLUSTER_NAME \
    --role-arn arn:aws:iam::$(aws sts get-caller-identity --query Account --output text --profile $AWS_PROFILE):role/LabRole \
    --resources-vpc-config subnetIds=$(aws ec2 describe-subnets --filters "Name=default-for-az,Values=true" --query 'Subnets[0:2].SubnetId' --output text --profile $AWS_PROFILE | tr '\t' ','),securityGroupIds=$(aws ec2 describe-security-groups --filters "Name=group-name,Values=default" --query 'SecurityGroups[0].GroupId' --output text --profile $AWS_PROFILE) \
    --region $AWS_REGION \
    --profile $AWS_PROFILE

# Aguardar cluster
aws eks wait cluster-active --name $CLUSTER_NAME --region $AWS_REGION --profile $AWS_PROFILE

# Configurar kubectl
aws eks update-kubeconfig --name $CLUSTER_NAME --region $AWS_REGION --profile $AWS_PROFILE

# Criar node group
aws eks create-nodegroup \
    --cluster-name $CLUSTER_NAME \
    --nodegroup-name fiap-nodegroup \
    --node-role arn:aws:iam::$(aws sts get-caller-identity --query Account --output text --profile $AWS_PROFILE):role/LabRole \
    --subnets $(aws ec2 describe-subnets --filters "Name=default-for-az,Values=true" --query 'Subnets[0:2].SubnetId' --output text --profile $AWS_PROFILE | tr '\t' ' ') \
    --instance-types t3.medium \
    --scaling-config minSize=2,maxSize=4,desiredSize=2 \
    --region $AWS_REGION \
    --profile $AWS_PROFILE

# Aguardar node group
aws eks wait nodegroup-active --cluster-name $CLUSTER_NAME --nodegroup-name fiap-nodegroup --region $AWS_REGION --profile $AWS_PROFILE
```

---

## 📦 Deploy Kubernetes

```bash
# Atualizar deployment com ECR URI
sed -i.bak "s|<SEU_ECR_URI>|$ECR_URI|g" k8s/deployment.yaml

# Aplicar manifestos
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml

# Obter URL do LoadBalancer
export LB_URL=$(kubectl get service fiapstore-api-service -n fiap-store -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "URL: http://$LB_URL"
```

---

## 🔍 Comandos de Verificação

```bash
# Ver todos os recursos
kubectl get all -n fiap-store

# Ver pods
kubectl get pods -n fiap-store

# Ver services
kubectl get svc -n fiap-store

# Ver deployments
kubectl get deployments -n fiap-store

# Ver HPA
kubectl get hpa -n fiap-store

# Ver nodes
kubectl get nodes

# Ver namespaces
kubectl get namespaces
```

---

## 📊 Monitoramento

```bash
# Instalar Metrics Server
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Ver métricas de nodes
kubectl top nodes

# Ver métricas de pods
kubectl top pods -n fiap-store

# Ver logs de um pod
kubectl logs -f <POD_NAME> -n fiap-store

# Ver logs de todos os pods
kubectl logs -l app=fiapstore-api -n fiap-store --tail=50

# Ver eventos
kubectl get events -n fiap-store --sort-by='.lastTimestamp'

# Descrever pod
kubectl describe pod <POD_NAME> -n fiap-store

# Descrever service
kubectl describe service fiapstore-api-service -n fiap-store

# Descrever HPA
kubectl describe hpa fiapstore-api-hpa -n fiap-store
```

---

## 🧪 Testes

```bash
# Health check
curl http://$LB_URL/health

# Info
curl http://$LB_URL/info | jq

# Listar produtos
curl http://$LB_URL/api/produtos | jq

# Buscar produto
curl http://$LB_URL/api/produtos/1 | jq

# Criar produto
curl -X POST http://$LB_URL/api/produtos \
    -H "Content-Type: application/json" \
    -d '{"nome":"Teste","descricao":"Produto teste","preco":99.90,"estoque":10}' | jq

# Swagger
echo "Swagger: http://$LB_URL/swagger"
```

---

## 🔄 Operações

```bash
# Escalar deployment
kubectl scale deployment fiapstore-api --replicas=5 -n fiap-store

# Atualizar imagem (rebuild com AMD64 antes)
docker buildx build --platform linux/amd64 -t fiapstore-api:v2 . --load
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text --profile fiapaws)
export ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.us-east-1.amazonaws.com/fiapstore-api"
docker tag fiapstore-api:v2 ${ECR_URI}:v2
docker push ${ECR_URI}:v2
kubectl set image deployment/fiapstore-api fiapstore-api=${ECR_URI}:v2 -n fiap-store

# Ver status do rollout
kubectl rollout status deployment/fiapstore-api -n fiap-store

# Ver histórico de rollouts
kubectl rollout history deployment/fiapstore-api -n fiap-store

# Fazer rollback
kubectl rollout undo deployment/fiapstore-api -n fiap-store

# Reiniciar deployment
kubectl rollout restart deployment/fiapstore-api -n fiap-store

# Pausar rollout
kubectl rollout pause deployment/fiapstore-api -n fiap-store

# Retomar rollout
kubectl rollout resume deployment/fiapstore-api -n fiap-store
```

---

## 🧹 Limpeza

```bash
# Deletar recursos K8s
kubectl delete -f k8s/hpa.yaml
kubectl delete -f k8s/service.yaml
kubectl delete -f k8s/deployment.yaml
kubectl delete -f k8s/configmap.yaml
kubectl delete -f k8s/namespace.yaml

# Deletar node group
aws eks delete-nodegroup --cluster-name $CLUSTER_NAME --nodegroup-name fiap-nodegroup --region $AWS_REGION --profile $AWS_PROFILE
aws eks wait nodegroup-deleted --cluster-name $CLUSTER_NAME --nodegroup-name fiap-nodegroup --region $AWS_REGION --profile $AWS_PROFILE

# Deletar cluster
aws eks delete-cluster --name $CLUSTER_NAME --region $AWS_REGION --profile $AWS_PROFILE
aws eks wait cluster-deleted --name $CLUSTER_NAME --region $AWS_REGION --profile $AWS_PROFILE

# Deletar ECR
aws ecr batch-delete-image --repository-name $ECR_REPO_NAME --image-ids imageTag=latest --region $AWS_REGION --profile $AWS_PROFILE
aws ecr delete-repository --repository-name $ECR_REPO_NAME --force --region $AWS_REGION --profile $AWS_PROFILE
```

---

## 🐛 Troubleshooting

```bash
# Ver logs de um pod específico
kubectl logs <POD_NAME> -n fiap-store

# Ver logs anteriores (se pod crashou)
kubectl logs <POD_NAME> -n fiap-store --previous

# Executar comando dentro do pod
kubectl exec -it <POD_NAME> -n fiap-store -- /bin/sh

# Port forward para testar localmente
kubectl port-forward <POD_NAME> 8080:8080 -n fiap-store

# Ver configuração do deployment
kubectl get deployment fiapstore-api -n fiap-store -o yaml

# Ver configuração do service
kubectl get service fiapstore-api-service -n fiap-store -o yaml

# Editar deployment
kubectl edit deployment fiapstore-api -n fiap-store

# Ver recursos do cluster
kubectl describe nodes

# Ver uso de recursos
kubectl top nodes
kubectl top pods -n fiap-store

# Ver todos os eventos do cluster
kubectl get events --all-namespaces --sort-by='.lastTimestamp'
```

---

## 📋 Informações Úteis

```bash
# Ver versão do kubectl
kubectl version --client

# Ver versão do cluster
kubectl version

# Ver contexto atual
kubectl config current-context

# Ver todos os contextos
kubectl config get-contexts

# Mudar de contexto
kubectl config use-context <CONTEXT_NAME>

# Ver configuração do kubectl
kubectl config view

# Ver API resources disponíveis
kubectl api-resources

# Ver API versions
kubectl api-versions
```

---

## 🎯 Atalhos Úteis

```bash
# Alias para kubectl
alias k=kubectl

# Alias para namespace
alias kn='kubectl -n fiap-store'

# Ver pods
k get po -n fiap-store

# Ver services
k get svc -n fiap-store

# Ver deployments
k get deploy -n fiap-store

# Ver tudo
k get all -n fiap-store

# Logs com follow
k logs -f <POD_NAME> -n fiap-store

# Descrever
k describe pod <POD_NAME> -n fiap-store
```

---

## 📊 Watch Commands

```bash
# Watch pods
kubectl get pods -n fiap-store --watch

# Watch services
kubectl get services -n fiap-store --watch

# Watch HPA
kubectl get hpa -n fiap-store --watch

# Watch nodes
kubectl get nodes --watch

# Watch events
kubectl get events -n fiap-store --watch
```

---

## 🔐 Segurança

```bash
# Ver service accounts
kubectl get serviceaccounts -n fiap-store

# Ver roles
kubectl get roles -n fiap-store

# Ver role bindings
kubectl get rolebindings -n fiap-store

# Ver secrets
kubectl get secrets -n fiap-store

# Ver configmaps
kubectl get configmaps -n fiap-store
```

---

## 💾 Backup e Restore

```bash
# Backup de um recurso
kubectl get deployment fiapstore-api -n fiap-store -o yaml > deployment-backup.yaml

# Backup de todos os recursos
kubectl get all -n fiap-store -o yaml > all-resources-backup.yaml

# Restore
kubectl apply -f deployment-backup.yaml
```

---

## 🎓 Comandos Educacionais

```bash
# Explicar um recurso
kubectl explain pod
kubectl explain deployment
kubectl explain service

# Ver campos de um recurso
kubectl explain pod.spec
kubectl explain deployment.spec.template

# Ver exemplos
kubectl create deployment --help
kubectl create service --help
```

---

**💡 Dica:** Salve este arquivo para consulta rápida durante a aula!
