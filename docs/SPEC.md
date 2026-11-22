# Specification

This specification defines what the calculator must do. Requirements (R1-R9) restate the brief.
Features (F1-F9) turn them into concrete rules and examples.

**How to read it.** Each feature was delivered on its own branch, tests first. Every test is tagged
with the requirement and feature it proves (`[Trait("Requirement", "R3")]`,
`[Trait("Feature", "F2")]`). The examples here are representative; the tests in `tests/` hold the
complete cases.

## Requirements

| ID | Requirement |
| -- | ----------- |
| R1 | A console application lets a user type an equation and prints the result. |
| R2 | An equation has any number of operands joined by `+ - * /`, starting and ending with a number. Parentheses are not required. |
| R3 | Results are mathematically correct: `*` and `/` bind tighter than `+` and `-`, and operators of equal priority run left to right (`10-2*6/4 = 7`). |
| R4 | Results are exact for decimal input (`0.1 + 0.2 = 0.3`) and look the same on every machine, whatever its language settings. |
| R5 | Invalid input and arithmetic failures produce clear, located error messages and never crash the program. |
| R6 | The interface fits how people really use a calculator: interactive, one-shot, scripted, explained, and in plain English. |
| R7 | Commercial quality: clear design, a code style enforced by tooling, automated tests and CI. |
| R8 | Documentation: a plain-text manual, a disclosure of AI use, and a 2-4 page design for a service with AI input. |
| R9 | A service proof of concept: the same engine over REST and MCP, natural-language input whose model output is validated before execution, and no paid API keys. |

## Grammar (R2)

An expression is a chain of terms joined by `+` or `-`. A term is a chain of factors joined by `*` or
`/`. This structure gives multiplication and division their higher priority.

```text
expression = term , { ( "+" | "-" ) , term } ;
term       = factor , { ( "*" | "/" ) , factor } ;
factor     = { "+" | "-" } , number ;                 (* unary signs *)
number     = digits , [ "." , [ digits ] ] | "." , digits ;
digits     = digit , { digit } ;
```

| Accepted | Rejected with a clear error |
| -------- | --------------------------- |
| Spaces and tabs between tokens | Spaces inside a number |
| `×` (U+00D7) for `*`, `÷` (U+00F7) for `/`, `−` (U+2212) for `-` | Parentheses, exponents, scientific notation |
| Repeated unary signs (`5 - - 3`) | Variables, thousands separators |

**Error positions** are zero-based indexes into the original text. A missing operand at the end of
the input is reported at `text.Length`. The length of a reported span is always at least 1.

## F1 - Parse and validate (R2, R5)

**API:** `Expression.Parse`, `Expression.ToString`

Parsing checks the whole input and returns a validated expression. `ToString` returns the canonical
form: single spaces, ASCII operators, normalised numbers and folded unary signs.

| Input | Canonical form |
| ----- | -------------- |
| `3.50 + .5` (with spaces around it) | `3.5 + 0.5` |
| `2 × 3 ÷ 4 − 1` | `2 * 3 / 4 - 1` |
| `5 - - 3` | `5 - -3` |
| `--5` | `5` |
| `-0` | `0` |
| `007` | `7` |

| Invalid input | Error code | Position, length |
| ------------- | ---------- | ---------------- |
| `2 + a` | UnexpectedCharacter | 4, 1 |
| `1.2.3` | InvalidNumber | 0, 5 |
| `10 +` | MissingOperand | 4, 1 |
| `10 20` | MissingOperator | 3, 2 |
| empty or blank | EmptyExpression | 0, 1 |

Rules:

- `Parse(null)` throws `ArgumentNullException`.
- **The leftmost error wins**, whether it is a bad character, a bad number or a grammar error.
  `10 * / 2 a` is MissingOperand at 5.
- **A syntax error beats an evaluation error.** `1/0 +` is MissingOperand, not DivisionByZero.
- **No silent rounding.** A literal that `decimal` cannot hold exactly (more than 28 decimals, or too
  many digits) is InvalidNumber, and its message says "exactly". An integer beyond `decimal.MaxValue`
  is Overflow.
- A `(` gets a message that mentions parentheses. `1,5` gets a message that quotes `'.'` as the
  decimal point.
- 50,000 operands and 10,000 unary signs parse without a stack overflow.
- Messages quote at most about 20 characters of the input. Control and invisible characters appear as
  `U+XXXX`. A surrogate pair, such as an emoji, counts as one character with length 2.

## F2 - Evaluate and format (R3, R4, R5)

**API:** `Expression.Evaluate`, `NumberFormatter.Format`

| Expression | Result | Steps |
| ---------- | ------ | ----- |
| `10-2*6/4` | 7 | `2 * 6 = 12`, `12 / 4 = 3`, `10 - 3 = 7` |
| `100/10/5` | 2 | |
| `0.1+0.2` | 0.3 | |
| `5--3` | 8 | |
| `7` | 7 | none |

| Failing expression | Error code | Position, length |
| ------------------ | ---------- | ---------------- |
| `10 / 0` | DivisionByZero | 3, 1 |
| `5 / -0` | DivisionByZero | 2, 1 |
| `79228162514264337593543950335 + 1` | Overflow | 30, 1 |
| `-79228162514264337593543950335 - 1` | Overflow | 31, 1 |

**Display rule.** `NumberFormatter.Format` uses the invariant culture. It rounds half away from zero
to `max(0, 25 - integer digits)` decimals, where integer digits is 0 for values between -1 and 1.
It never prints trailing zeros, exponents or group separators.

| Value | Displayed |
| ----- | --------- |
| `7.000m` | `7` |
| `-0.0m` | `0` |
| `1/3` | `0.3333333333333333333333333` |
| `1/3*3` | `1` |
| `12345678901234567890.123456789m` | `12345678901234567890.12346` |
| `1234567890123456789012344.5m` | `1234567890123456789012345` (midpoint rounds away from zero) |

Rules:

- **Literals are exact; results are rounded by `decimal`.** Each operation keeps 28-29 significant
  digits, so a result below 1e-28 becomes `0` (`0.0000000000000001 * 0.0000000000000001`). Only
  overflow is an error.
- A divisor is always a literal, so underflow can never cause a false division by zero.
- **A non-zero value is never displayed as `0`.** If display rounding would give zero, the exact value
  is printed instead.
- Formatting never depends on `CultureInfo.CurrentCulture`. This is tested under `uk-UA` and `de-DE`.

## F3 - Natural-language translation (R6, R9)

**API:** `NaturalLanguage.TryTranslate`

The translator works on words, ignoring case:

- It removes leading fillers (`what is`, `what's`, `calculate`, `compute`, `how much is`, `please`)
  and one trailing `?` or `.`.
- It maps words to operators: `plus` to `+`; `minus` to `-`; `times`, `multiplied by` and `x` to `*`;
  `divided by` and `over` to `/`.
- Numbers must be written as digits. Words may be separated by any whitespace.
- A word made only of digits, `.` and operator symbols (including the aliases) passes through
  unchanged. The translator never validates or evaluates; the parser does. For example,
  `what is 1.2.3 plus 1` becomes `1.2.3 + 1`, which the parser then rejects as InvalidNumber.

| Question | Translation |
| -------- | ----------- |
| `What is 10 minus 2 multiplied by 6 divided by 4?` | `10 - 2 * 6 / 4` |
| `how much is 9 over 3` | `9 / 3` |
| `what is 2+2?` | `2+2` |
| `What is 10 minus` | `10 -` (the parser then reports MissingOperand) |
| `what is the weather`, `ten plus two`, `subtract 2 from 10`, empty text | not translated (returns false) |

## F4 - Property and differential tests (R3, R4, R7)

**Tests only.**

- 2,000 random valid expressions are generated from a fixed seed. Each has 1-12 operands, integers
  from 0 to 999 or numbers with one decimal, and an optional unary minus.
- Each expression is evaluated by the engine and by an independent shunting-yard reference in the
  test project. The values must match exactly, and both must report any division by zero.
- Random whitespace never changes a result.
- `Parse(e.ToString())` gives the same text and the same value as `e`.
- A failure prints the seed and the expression, so it can be reproduced.

## F5 - Console: one-shot and piped modes (R1, R5, R6)

**Program:** `calc`

- **One-shot.** Arguments are joined with spaces. An argument is an option only if it is `-` or `--`
  followed by a letter; everything else is expression text.

  | Command | Output |
  | ------- | ------ |
  | `calc -5+3` | `-2` |
  | `calc --5` | `5` |
  | `calc 5 --3` | `8` |
  | `calc -` | MissingOperand |

- **Piped.** When stdin is redirected and no expression is given, each line is evaluated. Blank lines
  and lines starting with `#` are skipped, and a leading UTF-8 byte-order mark is ignored. Results go
  to stdout and diagnostics to stderr.
- **Questions.** Input that contains letters is first translated by `NaturalLanguage`. Stderr then
  shows `interpreted as: <text>`, even if the translation fails to parse.
  `calc "What is 10 minus 2 multiplied by 6 divided by 4?"` prints `7`.
- **Limits.** A piped line over 10,000 characters gives `line N: error[InputTooLong]: ...`, and
  processing continues. One-shot arguments over 10,000 characters in total give
  `error[InputTooLong]` with exit code 1 and no echo of the input.
- **Safe echo.** The echoed input is safe for the terminal and keeps its columns aligned: a tab
  becomes a space, and a control or format character becomes `?`.
- **Exit codes.** `0` when everything succeeded, `1` when at least one expression failed, `2` for a
  usage error such as `calc --bogus`.
- **Error format.** A two-space indent, with `^` repeated `Length` times. In piped mode, the first line
  starts with `line N:` and a space. For example, `printf '1+2\n10/0\n' | calc` prints `3`, then the error block
  for line 2, and exits with 1.

```text
error[MissingOperand]: expected a number after '+'
  10 + * 3
       ^
```

## F6 - Console: interactive mode and options (R1, R6)

- **Interactive.** With no arguments and stdin not redirected, `calc` starts a session: a banner line,
  then the prompt `>` followed by a space. Commands are `help`, `explain <expression>`, `exit` and `quit`. End of input
  (Ctrl+D) exits with 0. Errors never end the session, and the caret aligns under the typed text.
- **Options.** `-h` or `--help` prints usage to stdout and exits with 0. `--version` prints
  `calc 1.0.0` and exits with 0. `-e` or `--explain` prints each step (such as `2 * 6 = 12`) before the
  result, in any mode.
- `explain 10-2*6/4` prints the three steps, then `7`. `2 +` prints `error[MissingOperand]` and the
  caret line, then the next prompt.

## F7 - REST API (R9)

**Project:** `Calculator.Api`

| Request | Response |
| ------- | -------- |
| `POST /v1/evaluations` `{"expression":"10 - 2 * 6 / 4"}` | 200 `{"expression":"10 - 2 * 6 / 4","result":"7"}` |
| The same with `"explain":true` | 200, plus `"steps":[{"left":"2","operator":"*","right":"6","result":"12"},...]` |
| `POST /v1/evaluations` `{"expression":"10 / 0"}` | 422 Problem Details: `errorCode` `DivisionByZero`, `position` 3, `length` 1 |
| `POST /v1/evaluations/validate` `{"expression":"10 + * 3"}` | 200 `{"valid":false,"error":{"errorCode":"MissingOperand","position":5,"length":1,"message":...}}` |
| `POST /v1/evaluations/validate` with a valid expression | 200 `{"valid":true,"canonical":"10 - 2"}` |
| `GET /health` | 200 |

Rules:

- `GET /openapi/v1.json` declares 200, 400, 413 and 422 for both endpoints.
- Results and step values are strings, so JavaScript clients lose no precision.
- A missing or non-string `expression`, or an unknown JSON member (`"explian":true`), gives 400. A
  request body over 16 KB gives 413.
- An expression over 1,000 characters gives 422 `InputTooLong`, with `position` 1000 and `length`
  equal to the number of characters beyond the limit.
- Every error (400, 413, 422 and any unexpected 500) is `application/problem+json` with `status`,
  `title`, `instance` (`<METHOD> <path>`) and `traceId`. This is verified in the Production
  configuration.
- The console accepts longer input (10,000 characters) than the API (1,000). The console is a local
  scripting tool, while the API is shared and exposed to the network.

## F8 - MCP tools (R9)

**Endpoint:** `/mcp` (Streamable HTTP)

| Tool | Returns |
| ---- | ------- |
| `evaluate_expression(expression)` | Structured content `{expression, result, steps}`; the text block carries the same JSON |
| `validate_expression(expression)` | `{valid, canonical?, error?}` |

Rules:

- Errors are MCP tool errors (`isError`). Their text names the code, the zero-based `index` and the
  `length`. `10 + * 3` gives `MissingOperand`, `index 5`, `length 1`.
- 1,001 characters give `InputTooLong` at `index 1000`. 1,005 characters also give `length 5`, as in
  REST. `"expression": null` gives `MissingExpression`.
- The tool descriptions tell the assistant's model to translate the user's words into `+ - * /` only
  and never to compute the answer. Unsupported questions (grouping, powers, percentages) must be
  reported as unsupported, not rearranged.
- There is no authentication, because this is a local proof of concept. Requests whose `Host` is not
  `localhost`, `127.0.0.1` or `[::1]` are rejected, which protects against DNS rebinding.
  Deployments override the allowed hosts in configuration.

## F9 - Natural-language endpoint with a validated model fallback (R9)

**Endpoint:** `POST /v1/evaluations/natural-language` `{"question": "..."}`

How a question is answered:

1. If `NaturalLanguage.TryTranslate` succeeds, the result has `interpreter` set to `rules`.
2. Otherwise, if an `IChatClient` is registered, the model is asked for `{"expression": "..."}`, and
   the result has `interpreter` set to `llm`. The app registers a client when `Llm:Endpoint` and
   `Llm:Model` are set (any OpenAI-compatible server, such as a local Ollama). Tests register a fake.
3. The candidate expression must be non-blank, at most 1,000 characters, contain only digits, `.`,
   whitespace and `+ - * / × ÷ −`, and pass `Expression.Parse`. Only then is it evaluated. Anything
   else is `Uninterpretable`, so the endpoint cannot act as a general model proxy.

| Case | Response |
| ---- | -------- |
| `What is 10 minus 2 multiplied by 6 divided by 4?` | 200 `{"interpretedExpression":"10 - 2 * 6 / 4","result":"7","interpreter":"rules"}` |
| The rules fail and no model is configured | 422 `Uninterpretable`, with a hint to rephrase |
| The fake model returns `{"expression":"3 * 4"}` | 200, `interpreter` `llm`, result `12` |
| The fake model returns an injection, code, SQL, prose, non-JSON, oversized output or an empty expression | 422 `Uninterpretable` or the parser's error; nothing is evaluated |
| The model throws or times out | 503 Problem Details; never 500, and never raw model text |
| More than 30 requests per minute from one client, or more than 4 concurrent model calls | 429 with `Retry-After` |

Rules:

- `question` follows the F7 request rules: 400 for a missing or non-string value or unknown members,
  and 422 `InputTooLong` over 1,000 characters.
- A 422 for an interpreted expression includes `interpretedExpression` and `interpreter`. Its
  `position` and `length` refer to that expression, not to the question.
- The question is sent only as a user-role message. The system prompt treats it as untrusted data,
  forbids computing, simplifying or rearranging, and requires `{"expression": null}` for unsupported
  questions.
- Model calls are capped: a maximum number of output tokens and no retries. `Llm:TimeoutSeconds`
  defaults to 10.
- `Llm:Endpoint` and `Llm:Model` without an API key is a valid setup, for example a local Ollama. A
  malformed or non-HTTP endpoint stops the app at startup.
- Error details never reveal configuration keys. The API key is kept in user-secrets or in the
  `Llm__ApiKey` environment variable.
