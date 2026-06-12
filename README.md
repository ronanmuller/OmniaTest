# Ambev Developer Evaluation API

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

---

## Subindo a infraestrutura

```bash
docker-compose up ambev.developerevaluation.database ambev.developerevaluation.nosql ambev.developerevaluation.cache ambev.developerevaluation.messagebroker -d
```

Aguarde todos os containers ficarem **healthy** antes de iniciar a API.

| Serviço    | Porta |
|------------|-------|
| PostgreSQL | 5432  |
| MongoDB    | 27017 |
| Redis      | 6379  |
| RabbitMQ   | 5672  |
| RabbitMQ Management UI | 15672 |

---

## Iniciando a API

```bash
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

Ou pelo Visual Studio: `F5`.

As migrations são aplicadas automaticamente na inicialização.

Swagger disponível em: `http://localhost:5119/swagger`

---

## Autenticação

Todos os endpoints (exceto criação de usuário e login) exigem JWT.

**1. Crie um usuário**

`POST /api/Users`

```json
{
  "username": "joao",
  "email": "joao@teste.com",
  "password": "Teste@123!",
  "phone": "+5511999999999",
  "role": "Manager"
}
```

Roles aceitos: `None`, `Customer`, `Manager`, `Admin`.

> Email duplicado retorna `409 Conflict` com a mensagem `"User with email {email} already exists"`.

**2. Autentique**

`POST /api/Auth`

```json
{
  "email": "joao@teste.com",
  "password": "Teste@123!"
}
```

Copie o `token` da resposta. A resposta inclui `token`, `email`, `name` e `role`.

> Token expira em 8 horas. Após expirar, autentique novamente.

**3. Autorize no Swagger**

Clique em **Authorize** (cadeado no topo) → cole o token → **Authorize**.

> Requisições sem token retornam `401` com a mensagem `"Authentication required. Provide a valid Bearer token in the Authorization header."`.
> No Swagger, se o cadeado do endpoint estiver aberto 🔓, o token não foi aplicado — refaça o passo 3.

---

## Endpoints

### Usuários

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/Users` | Criar usuário |
| GET | `/api/Users/{id}` | Buscar usuário por ID |
| DELETE | `/api/Users/{id}` | Remover usuário |

### Produtos

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/Products` | Criar produto |
| GET | `/api/Products/{id}` | Buscar produto por ID |
| GET | `/api/Products` | Listar produtos (paginado) |
| PUT | `/api/Products/{id}` | Atualizar produto |
| DELETE | `/api/Products/{id}` | Remover produto |
| GET | `/api/Products/categories` | Listar categorias disponíveis |

### Vendas

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/Sales` | Criar venda |
| GET | `/api/Sales/{id}` | Buscar venda por ID |
| GET | `/api/Sales` | Listar vendas (paginado) |
| PUT | `/api/Sales/{id}` | Atualizar venda |
| DELETE | `/api/Sales/{id}` | Cancelar venda |
| PATCH | `/api/Sales/{id}/items/{itemId}/cancel` | Cancelar item da venda |
| POST | `/api/Sales/resync-read-model` | Reprojetar todas as vendas do PostgreSQL no MongoDB |

> O `GET /api/Sales` lê do MongoDB (read model). Se o MongoDB estiver vazio ou desatualizado, use `POST /api/Sales/resync-read-model` para sincronizar.

### Carrinhos

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/Carts` | Criar carrinho |
| GET | `/api/Carts/{id}` | Buscar carrinho por ID |
| GET | `/api/Carts` | Listar carrinhos (paginado) |
| PUT | `/api/Carts/{id}` | Atualizar carrinho |
| DELETE | `/api/Carts/{id}` | Remover carrinho |

---

## Criando uma venda

`POST /api/Sales`

```json
{
  "saleNumber": "VND-001",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "Cliente Teste",
  "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "branchName": "Filial SP",
  "items": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "productTitle": "Cerveja Brahma 350ml",
      "quantity": 5,
      "unitPrice": 4.50
    }
  ]
}
```

> `date` não é enviado — o servidor define automaticamente como o momento da criação (UTC).

**Regras de desconto aplicadas automaticamente:**

| Quantidade por item | Desconto |
|---------------------|----------|
| Menos de 4 | Sem desconto |
| 4 a 9 | 10% |
| 10 a 20 | 20% |
| Mais de 20 | Não permitido (400 Bad Request) |

---

## Arquitetura

```
HTTP Request
    └─► WebApi (Controllers)
            └─► MediatR (Commands/Queries)
                    └─► Application (Handlers)
                            ├─► Domain (Entities, Rules, Events)
                            ├─► ORM (PostgreSQL via EF Core)
                            │       └─► OutboxMessages (evento salvo atomicamente)
                            └─► OutboxProcessor (BackgroundService)
                                    └─► RabbitMQ (Rebus)
                                            └─► EventHandlers
                                                    └─► MongoDB (Read Models)
```

- **PostgreSQL** — fonte de verdade (write side): Users, Sales, SaleItems, Products, Carts
- **MongoDB** — read model (leitura de vendas via `GET /api/Sales`): projetado pelo `SaleCreatedEventHandler`
- **Redis** — cache de produtos (TTL 5 min)
- **RabbitMQ** — transporte de eventos de domínio via Rebus
- **Outbox Pattern** — evento é salvo no PostgreSQL na mesma transação da venda; o `OutboxProcessor` publica no RabbitMQ a cada 5s, garantindo entrega sem perda mesmo com falha de rede

---

## Executando os testes

### Todos os testes (exceto E2E)

```bash
dotnet test --filter "Category!=E2E"
```

### Suite completa com E2E (requer Docker)

```bash
# 1. Suba os containers primeiro
docker-compose up ambev.developerevaluation.database ambev.developerevaluation.nosql ambev.developerevaluation.cache ambev.developerevaluation.messagebroker -d

# 2. Execute todos os testes
dotnet test
```

---

## Suites de teste

| Suite | Projeto | Quantidade | O que testa |
|-------|---------|-----------|-------------|
| **Unit** | `Ambev.DeveloperEvaluation.Unit` | 123 | Regras de domínio (Sale, SaleItem, User), handlers da Application, especificações, validadores — sem I/O |
| **Integration** | `Ambev.DeveloperEvaluation.Integration` | 45 | Repositórios contra PostgreSQL real (via Docker) — mapeamentos EF, queries, transações |
| **Functional** | `Ambev.DeveloperEvaluation.Functional` | 26 | API completa via `WebApplicationFactory` com banco real — autenticação, status HTTP, body das respostas |
| **E2E** | `Ambev.DeveloperEvaluation.E2E` | 3 | Fluxo assíncrono completo: HTTP → PostgreSQL → OutboxProcessor → RabbitMQ → EventHandler → MongoDB |

### O que cada suite pega que a anterior não pega

- **Unit** não detecta: mapeamentos EF incorretos, connection strings erradas, queries lentas
- **Integration** não detecta: endpoints HTTP com status errado, middlewares de autenticação, serialização JSON
- **Functional** não detecta: eventos de domínio não chegando ao MongoDB, OutboxProcessor falhando silenciosamente
- **E2E** detecta: `SaleCreatedEvent` sem itens (Items = []), `OutboxProcessor` com coluna faltando no banco, handler não projetando campos corretos no MongoDB, roteamento RabbitMQ incorreto

### Cobertura de relatório

```bash
# Windows
coverage-report.bat

# Linux/Mac
./coverage-report.sh
```
