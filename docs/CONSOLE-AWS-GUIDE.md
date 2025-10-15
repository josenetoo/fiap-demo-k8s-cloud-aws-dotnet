# 🌐 Guia Completo - Console AWS

> **Instruções detalhadas para executar todos os passos via Console AWS**

---

## 📋 Índice

1. [Criar Repositório ECR](#1-criar-repositório-ecr)
2. [Criar Cluster EKS](#2-criar-cluster-eks)
3. [Criar Node Group](#3-criar-node-group)
4. [Verificar Recursos](#4-verificar-recursos)
5. [Deletar Recursos](#5-deletar-recursos)

---

## 1. Criar Repositório ECR

### Passo a Passo

1. **Acessar o Console AWS**
   - Faça login no AWS Console
   - Certifique-se de estar na região **us-east-1** (N. Virginia)

2. **Navegar para ECR**
   - No campo de busca, digite **"ECR"**
   - Clique em **"Elastic Container Registry"**

3. **Criar Repositório**
   - Clique em **"Get Started"** (primeira vez) ou **"Create repository"**

4. **Configurar Repositório**
   
   **General settings:**
   - **Visibility settings**: Private
   - **Repository name**: `fiapstore-api`
   
   **Image tag mutability:**
   - Selecione: **Mutable** (permite sobrescrever tags)
   
   **Image scan settings:**
   - Deixe **desabilitado** (opcional para economizar)
   
   **Encryption settings:**
   - Selecione: **AES-256** (padrão)
   
   **KMS encryption:**
   - Deixe **desabilitado**

5. **Criar**
   - Clique em **"Create repository"**
   - Aguarde a confirmação

6. **Copiar URI**
   - Na lista de repositórios, localize **fiapstore-api**
   - Copie o **URI** (formato: `ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/fiapstore-api`)
   - Salve em um arquivo de texto para usar depois

### Comandos CLI Necessários

Mesmo usando o Console, você precisará do CLI para fazer push da imagem:

```bash
# Definir variável com o URI copiado
export ECR_URI=<COLE_O_URI_AQUI>

# Login no ECR
aws ecr get-login-password --region us-east-1 --profile fiapaws | \
    docker login --username AWS --password-stdin $ECR_URI

# Tag e push da imagem
docker tag fiapstore-api:latest $ECR_URI:latest
docker push $ECR_URI:latest
```

### Verificar Imagem

1. No Console ECR, clique no repositório **fiapstore-api**
2. Você verá a imagem com tag **latest**
3. Verifique o tamanho e a data de push

---

## 2. Criar Cluster EKS

### Passo a Passo

1. **Navegar para EKS**
   - No campo de busca, digite **"EKS"**
   - Clique em **"Elastic Kubernetes Service"**

2. **Iniciar Criação**
   - Clique em **"Add cluster"**
   - Selecione **"Create"**

### Step 1: Configure cluster

**Cluster configuration:**
- **Name**: `fiap-eks-cluster`
- **Kubernetes version**: 1.28 ou mais recente
- **Cluster service role**: `LabRole`
  - ⚠️ Se não aparecer, atualize a página

**Cluster endpoint access:**
- Deixe o padrão: **Public and private**

**Secrets encryption:**
- Deixe **desabilitado**

**Tags (opcional):**
- Key: `Environment` | Value: `Education`
- Key: `Project` | Value: `FIAP-PosGrad`

Clique em **"Next"**

### Step 2: Specify networking

**Networking:**
- **VPC**: Selecione a **VPC padrão** (default)
- **Subnets**: Selecione **pelo menos 2 subnets** em AZs diferentes
  - ✅ Exemplo: us-east-1a e us-east-1b
  - ⚠️ Certifique-se de que são subnets **públicas**

**Security groups:**
- Selecione o **security group padrão** (default)

**Cluster endpoint access:**
- Selecione: **Public**
- Isso permite acesso via kubectl de qualquer lugar

**Advanced settings:**
- Deixe os valores padrão

Clique em **"Next"**

### Step 3: Configure observability

**Control plane logging:**
- Deixe **todos desabilitados** para economizar custos
- Em produção, você habilitaria: API, Audit, Authenticator

**Prometheus metrics:**
- Deixe **desabilitado**

Clique em **"Next"**

### Step 4: Select add-ons

**Add-ons:**
- Mantenha os add-ons padrão selecionados:
  - ✅ Amazon VPC CNI
  - ✅ kube-proxy
  - ✅ CoreDNS

**Version:**
- Use as versões padrão/recomendadas

Clique em **"Next"**

### Step 5: Configure selected add-ons settings

- Mantenha as configurações padrão
- Clique em **"Next"**

### Step 6: Review and create

1. **Revise todas as configurações:**
   - Nome do cluster
   - Versão do Kubernetes
   - VPC e subnets
   - Security groups
   - Add-ons

2. **Criar cluster:**
   - Clique em **"Create"**

3. **Aguardar criação:**
   - ⏳ Isso leva **10-15 minutos**
   - O status mudará de "Creating" para **"Active"**
   - Você pode acompanhar na lista de clusters

### Configurar kubectl (CLI necessário)

```bash
# Atualizar kubeconfig
aws eks update-kubeconfig \
    --name fiap-eks-cluster \
    --region us-east-1 \
    --profile fiapaws

# Verificar conexão
kubectl cluster-info
```

---

## 3. Criar Node Group

### Passo a Passo

1. **Acessar o Cluster**
   - No Console EKS, clique no cluster **fiap-eks-cluster**
   - Aguarde até o status estar **"Active"**

2. **Navegar para Compute**
   - Clique na aba **"Compute"**
   - Você verá que não há node groups ainda

3. **Adicionar Node Group**
   - Clique em **"Add node group"**

### Step 1: Configure node group

**Node group configuration:**
- **Name**: `fiap-nodegroup`

**Node IAM role:**
- Selecione: `LabRole`
- ⚠️ Se não aparecer, atualize a página

**Tags (opcional):**
- Key: `Environment` | Value: `Education`

Clique em **"Next"**

### Step 2: Set compute and scaling configuration

**Node group compute configuration:**

**AMI type:**
- Selecione: **Amazon Linux 2 (AL2_x86_64)**

**Capacity type:**
- Selecione: **On-Demand**
- ⚠️ Não use Spot para este lab (pode ser terminado)

**Instance types:**
- Clique em **"Add instance type"**
- Selecione: **t3.medium**
- Remova outros tipos se houver

**Disk size:**
- Mantenha: **20 GiB**

**Node group scaling configuration:**
- **Minimum size**: `2`
- **Maximum size**: `4`
- **Desired size**: `2`

**Node group update configuration:**
- Deixe os valores padrão

Clique em **"Next"**

### Step 3: Specify networking

**Subnets:**
- Selecione as **mesmas subnets** usadas no cluster
- Devem ser subnets públicas

**Configure remote access to nodes:**
- Deixe **desabilitado**
- Não precisamos SSH nos nodes para este lab

**SSH key pair (opcional):**
- Deixe em branco

**Allow remote access from:**
- Deixe em branco

Clique em **"Next"**

### Step 4: Review and create

1. **Revise as configurações:**
   - Nome do node group
   - IAM role
   - Instance type (t3.medium)
   - Scaling (2-4 nodes)
   - Subnets

2. **Criar node group:**
   - Clique em **"Create"**

3. **Aguardar criação:**
   - ⏳ Isso leva **3-5 minutos**
   - O status mudará de "Creating" para **"Active"**

### Verificar Nodes (CLI necessário)

```bash
# Ver nodes
kubectl get nodes

# Ver detalhes dos nodes
kubectl get nodes -o wide

# Deve mostrar 2 nodes em status "Ready"
```

---

## 4. Verificar Recursos

### ECR - Verificar Imagens

1. **Console ECR**
   - Navegue para **ECR**
   - Clique no repositório **fiapstore-api**

2. **Verificar imagem:**
   - Você deve ver a imagem com tag **latest**
   - Verifique:
     - ✅ Image tag
     - ✅ Size (deve ser ~200MB)
     - ✅ Pushed at (data/hora)
     - ✅ Image URI

### EKS - Verificar Cluster

1. **Console EKS**
   - Navegue para **EKS**
   - Clique em **Clusters**

2. **Verificar cluster:**
   - Status: **Active** ✅
   - Kubernetes version: 1.28+
   - Endpoint: URL do API server
   - Networking: VPC, subnets, security groups

3. **Verificar Node Group:**
   - Aba **Compute**
   - Node group: **fiap-nodegroup**
   - Status: **Active** ✅
   - Desired size: 2
   - Current size: 2

4. **Verificar Nodes:**
   - Você verá 2 instâncias EC2 listadas
   - Status: **Ready**

### EC2 - Verificar Instâncias

1. **Console EC2**
   - Navegue para **EC2**
   - Clique em **Instances**

2. **Verificar instâncias do EKS:**
   - Você verá 2 instâncias t3.medium
   - Nome: Contém "fiap-eks-cluster" e "fiap-nodegroup"
   - State: **Running** ✅
   - Instance type: t3.medium

### VPC - Verificar Networking

1. **Console VPC**
   - Navegue para **VPC**

2. **Verificar recursos:**
   - VPC padrão sendo usada
   - Subnets públicas
   - Internet Gateway
   - Route Tables

### CloudFormation (Opcional)

O EKS cria stacks do CloudFormation automaticamente:

1. **Console CloudFormation**
   - Navegue para **CloudFormation**
   - Você verá stacks relacionados ao EKS

---

## 5. Deletar Recursos

### ⚠️ IMPORTANTE: Execute na ordem correta!

### 5.1 Deletar Recursos Kubernetes (CLI)

```bash
# Deletar todos os recursos do namespace
kubectl delete namespace fiap-store

# Ou deletar individualmente
kubectl delete -f k8s/hpa.yaml
kubectl delete -f k8s/service.yaml
kubectl delete -f k8s/deployment.yaml
kubectl delete -f k8s/configmap.yaml
kubectl delete -f k8s/namespace.yaml
```

### 5.2 Deletar Node Group (Console)

1. **Console EKS**
   - Clique no cluster **fiap-eks-cluster**
   - Aba **Compute**

2. **Deletar node group:**
   - Selecione **fiap-nodegroup**
   - Clique em **"Delete"**
   - Digite o nome para confirmar: `fiap-nodegroup`
   - Clique em **"Delete"**

3. **Aguardar exclusão:**
   - ⏳ Isso leva **3-5 minutos**
   - Aguarde até desaparecer da lista

### 5.3 Deletar Cluster EKS (Console)

1. **Console EKS**
   - Na lista de clusters, selecione **fiap-eks-cluster**

2. **Deletar cluster:**
   - Clique em **"Delete"**
   - Digite o nome para confirmar: `fiap-eks-cluster`
   - Clique em **"Delete"**

3. **Aguardar exclusão:**
   - ⏳ Isso leva **5-10 minutos**
   - Aguarde até desaparecer da lista

### 5.4 Deletar Imagens ECR (Console)

1. **Console ECR**
   - Clique no repositório **fiapstore-api**

2. **Deletar imagens:**
   - Selecione todas as imagens
   - Clique em **"Delete"**
   - Confirme a exclusão

### 5.5 Deletar Repositório ECR (Console)

1. **Console ECR**
   - Na lista de repositórios, selecione **fiapstore-api**

2. **Deletar repositório:**
   - Clique em **"Delete"**
   - Digite `delete` para confirmar
   - Clique em **"Delete"**

### 5.6 Verificar Instâncias EC2 (Console)

1. **Console EC2**
   - Navegue para **EC2** > **Instances**

2. **Verificar:**
   - As instâncias do node group devem estar **Terminated**
   - Se ainda estiverem rodando, aguarde alguns minutos

### 5.7 Verificar LoadBalancers (Console)

1. **Console EC2**
   - Navegue para **Load Balancers**

2. **Deletar LoadBalancer (se existir):**
   - Selecione o LoadBalancer criado pelo Kubernetes
   - Actions > **Delete**

### Verificação Final

Execute estes comandos para garantir que tudo foi deletado:

```bash
# Verificar clusters EKS
aws eks list-clusters --region us-east-1 --profile fiapaws

# Verificar repositórios ECR
aws ecr describe-repositories --region us-east-1 --profile fiapaws

# Verificar instâncias EC2
aws ec2 describe-instances \
    --filters "Name=instance-state-name,Values=running" \
    --region us-east-1 \
    --profile fiapaws \
    --query 'Reservations[].Instances[].{ID:InstanceId,Type:InstanceType,State:State.Name}'
```

**✅ Tudo limpo!** Seu budget está preservado.

---

## 💡 Dicas Importantes

### Durante a Criação

1. **Paciência**: Criação de cluster leva tempo
2. **Região**: Sempre use **us-east-1**
3. **IAM Role**: Sempre use **LabRole**
4. **Subnets**: Use subnets públicas
5. **Acompanhe**: Fique de olho nos status

### Durante o Uso

1. **Monitore custos**: Use AWS Cost Explorer
2. **Verifique recursos**: Periodicamente no Console
3. **Logs**: Ative apenas se necessário (custa dinheiro)
4. **Instâncias**: Use t3.medium (suficiente para lab)

### Durante a Exclusão

1. **Ordem correta**: K8s → Node Group → Cluster → ECR
2. **Aguarde**: Cada passo leva alguns minutos
3. **Verifique**: Confirme que tudo foi deletado
4. **LoadBalancers**: Podem ficar órfãos, delete manualmente

---

## 🆘 Troubleshooting

### Cluster não cria

**Problema**: Erro ao criar cluster

**Soluções:**
- Verifique se está usando **LabRole**
- Verifique se selecionou **2+ subnets**
- Verifique se as subnets são **públicas**
- Tente em outra região (us-west-2)

### Node Group não cria

**Problema**: Erro ao criar node group

**Soluções:**
- Aguarde cluster estar **Active**
- Verifique se está usando **LabRole**
- Verifique se selecionou as **mesmas subnets** do cluster
- Tente instance type diferente (t3.small)

### Não encontra LabRole

**Problema**: LabRole não aparece na lista

**Soluções:**
- Atualize a página
- Verifique se o Learner Lab está **ativo** (indicador verde)
- Verifique no Console IAM se LabRole existe

### Erro de permissão

**Problema**: "You are not authorized to perform this operation"

**Soluções:**
- Verifique se o Learner Lab está ativo
- Verifique se está na região **us-east-1** ou **us-west-2**
- Algumas operações não são permitidas no Learner Lab

### Budget excedido

**Problema**: Recursos não criam por limite de budget

**Soluções:**
- Delete recursos não utilizados
- Use instâncias menores
- Não deixe recursos rodando entre sessões
- Monitore custos no AWS Budgets

---

## 📚 Recursos Adicionais

### Documentação AWS

- [Amazon EKS User Guide](https://docs.aws.amazon.com/eks/latest/userguide/)
- [Amazon ECR User Guide](https://docs.aws.amazon.com/ecr/latest/userguide/)
- [AWS Console Guide](https://docs.aws.amazon.com/awsconsolehelpdocs/)

### Vídeos Tutoriais

- [Getting Started with Amazon EKS](https://www.youtube.com/watch?v=p6xDCz00TxU)
- [AWS Console Navigation](https://www.youtube.com/watch?v=Ia-UEYYR44s)

---

**💡 Lembre-se:** O Console é ótimo para aprender e visualizar, mas CLI é melhor para automação e reprodutibilidade!
