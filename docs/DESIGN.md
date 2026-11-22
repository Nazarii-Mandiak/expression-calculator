# Design: from console calculator to a shared calculation service

This document describes how the calculator evolves into a service that many products can use,
including questions asked in plain English. Items marked **Built** exist and are tested in
`src/Calculator.Api`; items marked **Proposed** are the next steps.

> **Guiding principle: the language model translates, the engine calculates.** A model may propose an
> expression, but the deterministic engine validates and evaluates it exactly like typed input.

## 1. API design

### 1.1 Endpoints

| Endpoint | Purpose | Status |
| -------- | ------- | ------ |
| `POST /v1/evaluations` | Evaluate an expression; optionally return every step | Built |
| `POST /v1/evaluations/validate` | Check syntax only, for live validation or generated expressions | Built |
| `POST /v1/evaluations/natural-language` | Turn a question into an expression and evaluate it | Built |
| `/mcp` | The same capabilities as tools for AI assistants (Model Context Protocol) | Built |
| `POST /v1/jobs`, `GET /v1/jobs/{id}` | Long-running batches: `202 Accepted`, then poll or receive a webhook | Proposed |

### 1.2 Example payloads

Evaluate with steps:

```http
POST /v1/evaluations
{ "expression": "10-2*6/4", "explain": true }

200 OK
{ "expression": "10 - 2 * 6 / 4", "result": "7",
  "steps": [ { "left": "2",  "operator": "*", "right": "6", "result": "12" },
             { "left": "12", "operator": "/", "right": "4", "result": "3" },
             { "left": "10", "operator": "-", "right": "3", "result": "7" } ] }
```

Ask a question:

```http
POST /v1/evaluations/natural-language
{ "question": "What is 10 minus 2 multiplied by 6 divided by 4?" }

200 OK
{ "interpretedExpression": "10 - 2 * 6 / 4", "result": "7", "interpreter": "rules" }
```

An invalid expression returns a located error in the standard RFC 9457 Problem Details format:

```http
POST /v1/evaluations
{ "expression": "10 / 0" }

422 Unprocessable Entity   (application/problem+json)
{ "title": "The expression is invalid.", "status": 422, "detail": "division by zero",
  "errorCode": "DivisionByZero", "position": 3, "length": 1,
  "instance": "POST /v1/evaluations", "traceId": "00-518a94dc…-00" }
```

Three contract choices matter for every client:

- **Results are strings.** JavaScript would turn a 28-digit decimal into a lossy double.
- **Errors are located.** `errorCode`, `position` and `length` let any client underline the problem.
- **Contracts are strict.** Unknown JSON fields are rejected with 400, and the limits (1,000
  characters per expression, 16 KB per request) are part of the contract.

### 1.3 Versioning

- The major version is part of the path (`/v1`). Within a version, changes are additive only.
- A breaking change ships as `/v2` alongside `/v1`. `/v1` responses then carry `Deprecation` and
  `Sunset` headers, and per-client usage metrics show when `/v1` can be retired.
- The OpenAPI document is the contract (**Built**). CI compares it against the previous release, and
  client libraries are generated from it (**Proposed**).

## 2. AI integration

### 2.1 Accepting "What is 10 minus 2 multiplied by 6 divided by 4?"

The question goes to `POST /v1/evaluations/natural-language`. Deterministic rules translate common
wording (*plus*, *minus*, *multiplied by*, *divided by*) into operators. This example needs no model
call at all, so it is free, instant and reproducible (**Built**). The response always shows the
interpreted expression, so the user can see how the question was understood.

### 2.2 How a language model is integrated

```text
question ─► size limit ─► rules ──(not understood)──► LLM (optional)
                            │                              │
                            │ expression text              │ {"expression": "…"}
                            ▼                              ▼
                     Expression.Parse   (the strict grammar is the whitelist)
                            │
                            ▼
                     Evaluate ─► result + interpretedExpression
```

- **Fallback model (Built).** Questions the rules cannot translate go to a model through
  `IChatClient`. It works with any OpenAI-compatible endpoint, either a cloud model or a local one
  for on-premises customers, chosen by configuration. Without a configured model, the service uses
  the rules only.
- **Assistants via MCP (Built).** Assistants such as Claude Code use their own model to translate and
  call the `evaluate_expression` tool. The tool description forbids the model from computing the
  answer itself.

### 2.3 Validating the generated expression before execution

Model output is treated as untrusted input, never as code:

1. **Prompt.** The question is sent only as a user message and declared untrusted. The model must
   not compute or rearrange anything, must answer `null` when the question cannot be expressed, and
   runs with temperature 0 and a token cap.
2. **Shape.** The reply must be a JSON object with a single string field, `expression`.
3. **Alphabet and size.** The expression may contain only digits, `.`, spaces and the four operators,
   and at most 1,000 characters.
4. **Grammar.** The text must parse with the same strict grammar as typed input.
5. **Evaluation.** Only then does the deterministic engine evaluate it.

Tests use a fake model that returns prompt injections, code, SQL and malformed JSON. Every case must
be rejected with nothing evaluated (**Built**).

**Ambiguity.** Read with operator precedence, the example is `10 − (2 × 6 ÷ 4) = 7`. Read the way it is
spoken, it could be `((10 − 2) × 6) ÷ 4 = 12`. The service applies precedence and always echoes its
interpretation. A later version could ask the user to confirm (**Proposed**).

**Evaluations.** A golden set of questions with expected expressions, plus an adversarial set, runs
in CI whenever the prompt or the model changes (**Proposed**).

## 3. Architecture

### 3.1 Service boundaries

| Component | Responsibility |
| --------- | -------------- |
| Calculation engine | Library with no dependencies; immutable and thread-safe; the single source of truth |
| Calculation API | Stateless REST and MCP; scales horizontally |
| Language service | Model calls. Separate in production because of latency, cost and outages, and can be switched off for air-gapped customers. The proof of concept runs it in the same process |
| API gateway | Authentication, per-product quotas and rate limits |

### 3.2 Reuse across products

One engine is offered through three channels: a NuGet package for in-process use, REST with
generated clients, and MCP for AI assistants. Every product therefore gets the same results, the same
error codes and the same limits.

### 3.3 Deployment

- **Containers.** Images are built by the .NET SDK (`dotnet publish -t:PublishContainer`), which is
  configured in the project, and run on Kubernetes or Azure Container Apps.
- **On-premises.** Customers run the same images, or install the API as a Windows service.
- **Operations.** Health probes are built in, and a feature flag turns the model fallback on or off.

## 4. Quality and security

### 4.1 Testing strategy

**Built:** acceptance tests from the specification, a comparison with an independent evaluator on
2,000 random expressions, property tests, API tests in the Production configuration, tests through
a real MCP client, hostile-model tests and mutation testing (97% of injected bugs caught).
**Proposed for the service:** OpenAPI contract tests, load tests and model evaluation suites.

### 4.2 Monitoring and observability

- OpenTelemetry traces and metrics: latency and errors per endpoint and product, and the mix of
  `errorCode` values.
- Model metrics: calls, tokens, cost, timeouts, and how often parsing rejects a model's reply. A
  rising rejection rate is an early sign of model drift.
- A `traceId` in every error response, and no logging of questions or model replies (**Built**).

### 4.3 Cost management

- **Built:** rules before any model call, a token cap per call, no automatic retries, and
  per-client and global rate limits.
- **Proposed:** a small model by default, caching of repeated questions, and a cost budget per
  product.

### 4.4 Security

- **Access.** OAuth 2.0 client credentials with scopes per product (**Proposed**).
- **Input.** Bounded input sizes, a parser whose depth does not grow with input length, and no dynamic
  code execution (**Built**).
- **Hosting.** Host-header filtering and secrets kept outside configuration files (**Built**).
- **Supply chain.**
  - **Built:** no runtime dependencies in the engine; locked and audited packages that a human
    approves; CI actions pinned by commit hash.
  - **Proposed:** a software bill of materials (SBOM) and signed artefacts.
- **Threat model.** The OWASP API Security Top 10 and the OWASP Top 10 for LLM Applications serve as
  checklists.

## 5. Future roadmap

1. **Calculation service.** This proof of concept, hardened with a gateway, observability and
   service-level objectives.
2. **Tool registry.** Each calculation becomes a versioned tool with a JSON schema, exposed through
   REST and MCP from one definition. The model proposes arguments, validators check them, and the
   engine executes.
3. **Agentic workflows.** Multi-step plans across tools run on a durable workflow engine, with human
   approval for consequential steps and a full audit trail.
4. **Platform.** Evaluations and quality gates per tool, tenant isolation, a gateway that works with
   any model provider, on-premises bundles, and governance over which assistant may use which tool.

Through every stage, one boundary stays fixed: models plan and translate, and deterministic, tested
code calculates.
