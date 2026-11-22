# ADR 0006: Natural language: the model translates, the engine calculates

- **Status:** Accepted
- **Date:** 2026-11-21

## Context

People ask "what is 10 minus 2 times 3?", not `10 - 2 * 3`. A language model handles loose wording,
but it is non-deterministic, remote and poor at arithmetic, and its output can be influenced by an
attacker. The service must stay correct and safe when the model is missing, wrong or hostile.

## Decision

- **Text only.** The model only produces expression text. That text is parsed again by
  `Expression.Parse` (see ADR 0002), the engine does the arithmetic, and nothing generated is ever
  executed.
- **Rules first.** `NaturalLanguage.TryTranslate` is deterministic, word-based and local. The model is
  asked only when the rules fail. Responses say which one answered (`interpreter`: `rules` or `llm`),
  and a 422 includes the `interpretedExpression`.
- **Optional and provider-neutral.** The model path exists only when an `IChatClient` is registered,
  which happens when `Llm:Endpoint` and `Llm:Model` are set. Any OpenAI-compatible server works, and
  a local one needs no key. Keys are read only from user-secrets or `Llm__ApiKey`.
- **Strict reply contract.** Temperature is 0. The reply must be JSON of the form
  `{"expression": ...}` with at most 1,000 arithmetic characters; anything else is `Uninterpretable`.
  The prompt treats the question as untrusted data.
- **Bounded cost.** There is a 10-second timeout, a cap on output tokens and no retries, plus limits of
  30 requests per minute per address and 4 concurrent model calls. Failures return 503 or 429, never
  500.
- **MCP.** The assistant supplies its own model. The server only validates and evaluates, and it
  filters requests by `Host`.

## Consequences

### Benefits

- A model failure, an injection attempt or a missing model affects one endpoint, never the
  calculator itself.

### Trade-offs

- A valid but wrong expression, such as a mistranslated number, remains possible. Showing
  `interpretedExpression` lets the user catch it.
- The rate limiter keys on the connection address, so clients behind one proxy share a budget.
- The proof of concept has no authentication.

## Alternatives considered

| Option | Why not |
| ------ | ------- |
| A model for every question | Slower, costly and non-deterministic even for simple questions |
| Letting the model compute the result or write code | Models make arithmetic mistakes, and generated code would have to be executed |
| Function calling | More moving parts than a single strict JSON reply, for the same result |
| Regular-expression rules | Harder to read and extend than word-based rules |
