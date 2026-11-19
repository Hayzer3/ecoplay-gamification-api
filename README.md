# EcoPlay — API de gamificação (.NET)

API ASP.NET Core Minimal API para XP, progresso, missões, quiz e ranking. O app consulta a API com JWT emitido pelo serviço Java; o consumidor interno do Java registra eventos confirmados de entrega e identificação.

## Requisitos

- .NET 10 SDK
- Oracle FIAP, com acesso ao host `oracle.fiap.com.br:1521` e SID `ORCL`
- Chave pública RSA em PEM emitida pelo time Java

## Configuração local

Defina a connection string sem versionar credenciais. Para o Oracle da FIAP:

```powershell
$env:ConnectionStrings__Gamification = "User Id=<usuario>;Password=<senha>;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle.fiap.com.br)(PORT=1521))(CONNECT_DATA=(SID=ORCL)))"
$env:Jwt__PublicKeyPem = Get-Content .\java-public-key.pem -Raw
$env:Jwt__Issuer = "ecoplay-java"
$env:Jwt__Audience = "ecoplay-services"
$env:JavaApi__HealthUrl = "http://localhost:8080/actuator/health"
```

O valor `Jwt:PublicKeyPem` de exemplo é vazio; nesse caso, a API sobe com uma chave RSA efêmera e tokens reais do Java não serão aceitos. Configure a chave pública antes de integrar. A API nunca emite token e não recebe a chave privada.

## Banco e migrations

```powershell
dotnet restore
dotnet tool install --global dotnet-ef --version 10.0.7
dotnet ef database update --project src/EcoPlay.Gamification.Api
```

O EF Core cria as tabelas no schema do usuário Oracle configurado na connection string; não é necessário criar um schema separado. A primeira execução aplica as migrations Oracle e completa o catálogo com seis missões e 20 perguntas de exemplo. Migrations são específicas do banco; use apenas as migrations Oracle desta versão. O comando de atualização precisa da connection string Oracle configurada no ambiente.

## Executar

```powershell
dotnet run --project src/EcoPlay.Gamification.Api
```

Em `Development`, Swagger fica em `/swagger`. O endpoint `/health` verifica conectividade do banco e, quando `JavaApi:HealthUrl` estiver configurado, consulta também a API Java.

## Deploy no Render

Crie um Web Service ligado a este repositório, selecione **Docker** e use a raiz do repositório como contexto de build. O `Dockerfile` publica a API em modo Release e escuta na porta informada pelo Render. Configure `/health` como Health Check Path. O serviço Render precisa conseguir abrir conexão TCP na porta 1521 para `oracle.fiap.com.br`; confirme esse acesso com o administrador do serviço Oracle da FIAP antes do deploy.

Cadastre as configurações no painel do serviço em **Environment**, sem adicioná-las aos arquivos do repositório:

| Variável | Valor |
|---|---|
| `ConnectionStrings__Gamification` | Connection string Oracle, no formato usado na configuração local |
| `Jwt__PublicKeyPem` | Conteúdo PEM da chave pública RSA usada pelo Java |
| `Jwt__Issuer` | `ecoplay-java` |
| `Jwt__Audience` | `ecoplay-services` |
| `JavaApi__HealthUrl` | URL pública de `/actuator/health` do Java; opcional |

O issuer e a audience precisam corresponder aos tokens emitidos pelo Java. A API reconhece o claim `papel` para autorização e usa `userId`, com fallback para `sub`, como identificador do usuário. Nunca configure a chave privada RSA no Render para este serviço. Mantenha a connection string somente nas variáveis de ambiente.

## Endpoints

| Método | Rota | Acesso | Função |
|---|---|---|---|
| GET | `/api/progresso` | `CIDADAO` | XP, nível, kg entregues, medalhas e bairro |
| GET | `/api/missoes` | `CIDADAO` | Catálogo e progresso de missões |
| GET | `/api/quiz/hoje` | `CIDADAO` | Perguntas ativas, sem gabarito |
| POST | `/api/quiz/respostas` | `CIDADAO` | Confere resposta; 5 XP por acerto até 50 XP/dia |
| GET | `/api/ranking?bairro=Centro` | `CIDADAO` | Ranking por XP, com filtro opcional |
| POST | `/api/xp/creditos` | `SISTEMA` | Credita entrega ou identificação com `Idempotency-Key` |
| POST | `/api/missoes/reforco` | `SISTEMA` | Atribui missão após recusa, com `Idempotency-Key` |
| GET | `/health` | Público | Saúde da API, Oracle e serviço Java configurado |

Claims esperadas no JWT: `sub` com o ID do usuário, `papel` com `CIDADAO` ou `SISTEMA`, mais `iss` e `aud` conforme configuração. Assinatura aceita apenas RS256; expiração é obrigatória.

### Contrato de crédito

```http
POST /api/xp/creditos
Authorization: Bearer <jwt-de-servico>
Idempotency-Key: entrega-123
Content-Type: application/json

{"usuarioId":"45","origem":"ENTREGA","referencia":"entrega-123","pesoKg":4.2,"residuoEspecial":false}
```

O serviço calcula XP de entrega (10 por kg, mínimo 20 e máximo 200), registra o evento e armazena a resposta na mesma transação. Repetir chave e conteúdo devolve a resposta original; reutilizar a chave com payload diferente retorna `409 Conflict`. Uma chave única no SQL protege chamadas concorrentes. Identificação confirmada gera 2 XP, com teto de 20 por dia. A API não aceita que o chamador escolha a quantidade de XP.

O endpoint de quiz limita a 10 respostas por minuto por `sub` e concede no máximo 50 XP por dia. Ao ultrapassar o limite de chamadas, responde `429 Too Many Requests`.

## Testes

```powershell
dotnet test
```

Os testes xUnit cobrem regras de níveis e XP e usam `WebApplicationFactory` com SQLite em memória para verificar autenticação e replay idempotente sem duplicar lançamento. Para validar concorrência no Oracle FIAP, rode também uma verificação de integração em ambiente de desenvolvimento com a mesma connection string do backend.

## Contrato de integração com Java

- Java assina os JWTs com RSA; compartilha somente a chave pública com esta API.
- A API Java publica o evento após confirmação da cooperativa; o consumidor Java envia o crédito com uma chave estável como `entrega-{entregaId}`.
- Para entrega recusada, o consumidor envia `POST /api/missoes/reforco` com a mesma chave de evento, `usuarioId`, `missaoId` e motivo.
- O claim `bairro` do usuário é usado para alimentar seu perfil quando consulta `/api/progresso`, permitindo filtro do ranking sem duplicar cadastro do schema `core`.
- O serviço usa o schema do usuário Oracle configurado; os identificadores Java são armazenados como texto e não há dependência de chave estrangeira para o schema da aplicação Java.

## Limitações conhecidas da primeira versão

- Perguntas e missões são conteúdo inicial demonstrativo; revise textos e regras com a equipe.
- O schema não persiste imagem nem dado pessoal adicional. A API recebe apenas o ID de usuário do Java.
- Medalhas são cadastradas; no primeiro corte, a atribuição automática está ligada a primeira entrega, 10 kg e resíduo especial. Separação perfeita e sequência de acertos exigem receber e persistir eventos de recusa e sequência de quiz, respectivamente.
- O fator de CO₂ não foi inventado. Antes de expor esse número, a equipe deve escolher e citar um fator oficial e definir o método de cálculo.
