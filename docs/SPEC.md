# Specification

Requirements (R) are paraphrased from the project brief. Features (F) are delivered one pull request
each, test-first. Every acceptance row below becomes at least one automated test tagged with its
requirement and feature IDs.

## Requirements

| ID | Requirement |
|----|-------------|
| R1 | A console application lets a user type an equation and prints the result. |
| R2 | Equations contain any number of operands and the operators `+ - * /`, starting and ending with a number. Parentheses are not required. |
| R3 | Results are mathematically correct: `*` and `/` bind tighter than `+` and `-`; equal precedence evaluates left to right (`10-2*6/4 = 7`). |
| R4 | Numeric results are exact for decimal input (`0.1 + 0.2 = 0.3`) and displayed consistently on every machine, independent of OS culture. |
| R5 | Invalid input and arithmetic failures produce clear, located error messages and never crash the program. |
| R6 | The interface is designed for how people actually use a calculator: interactive, one-shot, scripted/piped, explainable, natural-language questions. |
| R7 | Commercial quality: clear design, consistent code style enforced by tooling, automated tests, CI. |
| R8 | Documentation: a plain-text manual, AI-usage disclosure, and a 2–4 page service/AI design document. |
| R9 | Service proof of concept: the same engine over REST and MCP, natural-language input with LLM output validated before execution, free to run (no paid API keys). |

## Grammar (R2)

```
expression = term , { ( "+" | "-" ) , term } ;
term       = factor , { ( "*" | "/" ) , factor } ;
factor     = { "+" | "-" } , number ;                 (* unary signs *)
number     = digits , [ "." , [ digits ] ] | "." , digits ;
digits     = digit , { digit } ;
```

- Aliases: `×` (U+00D7) = `*`, `÷` (U+00F7) = `/`, `−` (U+2212) = `-`.
- Spaces and tabs are allowed between tokens, never inside a number.
- Out of scope (rejected with a clear error): parentheses, exponents, scientific notation, variables,
  thousands separators.
- Positions are zero-based indexes into the original text. A missing operand at the end of the input
  is reported at `text.Length`. `Length` is always at least 1.

---

## F1 — Parse and validate (R2, R5) · `Expression.Parse`, `Expression.ToString`

Valid input and canonical form (single spaces, ASCII operators, numbers normalised, unary signs folded):

| Input | `ToString()` |
|-------|--------------|
| `10-2*6/4` | `10 - 2 * 6 / 4` |
| `  10 - 2 * 6 / 4  ` | `10 - 2 * 6 / 4` |
| `42` | `42` |
| `3.50 + .5` | `3.5 + 0.5` |
| `5. * 2` | `5 * 2` |
| `2 × 3 ÷ 4 − 1` | `2 * 3 / 4 - 1` |
| `-5 + 3` | `-5 + 3` |
| `2 * -3` | `2 * -3` |
| `5 - - 3` | `5 - -3` |
| `--5` | `5` |
| `+5` | `5` |
| `1\t+\t2` (tabs) | `1 + 2` |

Errors (`CalculationException`):

| Input | Code | Position | Length | Message must mention |
|-------|------|----------|--------|----------------------|
| `` (empty) | EmptyExpression | 0 | 1 | |
| `   ` | EmptyExpression | 0 | 1 | |
| `2 + a` | UnexpectedCharacter | 4 | 1 | `a` |
| `(1+2)` | UnexpectedCharacter | 0 | 1 | parentheses |
| `1,5` | UnexpectedCharacter | 1 | 1 | `.` (decimal point hint) |
| `1e3` | UnexpectedCharacter | 1 | 1 | |
| `1.2.3` | InvalidNumber | 0 | 5 | |
| `.` | InvalidNumber | 0 | 1 | |
| `79228162514264337593543950336` | Overflow | 0 | 29 | |
| `10 +` | MissingOperand | 4 | 1 | |
| `-` | MissingOperand | 1 | 1 | |
| `10 * / 2` | MissingOperand | 5 | 1 | |
| `* 3` | MissingOperand | 0 | 1 | |
| `10 20` | MissingOperator | 3 | 2 | |

More valid input:

| Input | `ToString()` |
|-------|--------------|
| `-0` | `0` |
| `007` | `7` |
| `0.0000000000000000000000000001` (28 decimals, exactly representable) | `0.0000000000000000000000000001` |
| 50,000 operands (`1 + 1 + … + 1`) | parses without error (no stack overflow) |
| 10,000 unary minus signs followed by `5` | `5` |

More errors:

| Input | Code | Position | Length | Message must mention |
|-------|------|----------|--------|----------------------|
| `0.00000000000000000000000000001` (29 decimals) | InvalidNumber | 0 | 31 | exactly |
| `1.00000000000000000000000000001` | InvalidNumber | 0 | 31 | exactly |
| `1 .5` | MissingOperator | 2 | 2 | |
| `* a` | MissingOperand | 0 | 1 | |
| `10 * / 2 a` | MissingOperand | 5 | 1 | |

Rules:

- `Parse(null)` throws `ArgumentNullException`.
- **The leftmost error wins**: input is validated left to right and the first problem is reported,
  whether it is a bad character, a bad number or a grammar error.
- **No silent rounding**: a literal that `decimal` cannot represent exactly (more significant or
  fractional digits than it can hold) is rejected as InvalidNumber, never rounded.
- A syntax error is always reported even if an earlier part would fail at evaluation (`1/0 +` →
  MissingOperand, not DivisionByZero).
- The `1,5` message must contain `'.'` (the quoted decimal point), not just a full stop.
- Error messages never echo more than about 20 characters of user input, and control or invisible
  characters are shown as `U+XXXX`; a surrogate pair (e.g. an emoji) is one character with Length 2.

## F2 — Evaluate and format (R3, R4, R5) · `Expression.Evaluate`, `NumberFormatter.Format`

| Input | Value | Steps (in order) |
|-------|-------|------------------|
| `10-2*6/4` | 7 | `2 * 6 = 12`, `12 / 4 = 3`, `10 - 3 = 7` |
| `2+3*4` | 14 | `3 * 4 = 12`, `2 + 12 = 14` |
| `10-5-2` | 3 | `10 - 5 = 5`, `5 - 2 = 3` |
| `100/10/5` | 2 | `100 / 10 = 10`, `10 / 5 = 2` |
| `2*3/4` | 1.5 | |
| `1+2+3+4+5` | 15 | |
| `0.1+0.2` | 0.3 | |
| `-5+3` | -2 | |
| `2*-3` | -6 | |
| `5--3` | 8 | |
| `7` | 7 | (none) |

| Input | Code | Position | Length |
|-------|------|----------|--------|
| `10 / 0` | DivisionByZero | 3 | 1 |
| `1 + 5 / 0.0` | DivisionByZero | 6 | 1 |
| `79228162514264337593543950335 + 1` | Overflow | 30 | 1 |
| `79228162514264337593543950335 * 2` | Overflow | 30 | 1 |

`NumberFormatter.Format` (invariant culture; rounded half away from zero to
`max(0, 25 − integer digits)` decimal places, where integer digits is 0 when |value| < 1; no trailing
zeros, exponent or group separators):

| Value | Text |
|-------|------|
| `2.5m` | `2.5` |
| `7.000m` | `7` |
| `0m`, `-0.0m` | `0` |
| `-2.5m` | `-2.5` |
| `0.0000001m` | `0.0000001` |
| result of `1/3` | `0.3333333333333333333333333` |
| result of `2/3` | `0.6666666666666666666666667` |
| result of `1/3*3` | `1` |
| `79228162514264337593543950335m` | `79228162514264337593543950335` |
| `12345678901234567890.123456789m` | `12345678901234567890.12346` |

More evaluation rows:

| Input | Expected |
|-------|----------|
| `79228162514264337593543950335 / 0.5` | Overflow at 30, length 1 |
| `-79228162514264337593543950335 - 1` | Overflow at 31, length 1 |
| `5 / -0` | DivisionByZero at 2, length 1 |
| `0.0000000000000001 * 0.0000000000000001` | value `0` (underflow, see rules) |

More formatting rows:

| Value | Text | Why |
|-------|------|-----|
| `1234567890123456789012344.5m` | `1234567890123456789012345` | midpoint rounds away from zero (not to even) |
| `-0.00000000000000000000000025m` | `-0.0000000000000000000000003` | negative midpoint |
| `-12345678901234567890.123456789m` | `-12345678901234567890.12346` | sign does not count as a digit |
| `9.999999999999999999999999999m` | `10` | carry into a new integer digit |
| `0.9999999999999999999999999999m` | `1` | |
| `7922816251426433759354395033.5m` | `7922816251426433759354395034` | near the top of the range |
| `0.0000000000000000000000000001m` | `0.0000000000000000000000000001` | a non-zero value is never shown as `0` |
| `-0.0000000000000000000000000001m` | `-0.0000000000000000000000000001` | same, negative |

Rules:

- **Literals are exact; results are rounded by `decimal`.** Each operation's result keeps decimal's
  28–29 significant digits, so a result smaller than 1e-28 becomes `0` and digits beyond that
  precision are lost. Only overflow is an error. (A divisor is always a literal in this grammar, so an
  underflowed intermediate can never cause a false division-by-zero.)
- **A non-zero value is never displayed as `0`**: if display rounding would produce zero, the exact
  value is printed instead.
- Formatting must not depend on `CultureInfo.CurrentCulture` (verified under `uk-UA` and `de-DE`).

## F3 — Natural-language translation (R6, R9) · `NaturalLanguage.TryTranslate`

Case-insensitive. Strips leading fillers (`what is`, `what's`, `calculate`, `compute`, `how much is`,
`please`) and trailing `?`/`.`. Maps `plus`→`+`, `minus`→`-`, `times`/`multiplied by`/`x`→`*`,
`divided by`/`over`→`/`. Numbers must be written in digits. Symbols are passed through. It never
evaluates or validates arithmetic (the parser does).

| Input | Result |
|-------|--------|
| `What is 10 minus 2 multiplied by 6 divided by 4?` | `10 - 2 * 6 / 4` |
| `what's 3 plus 4` | `3 + 4` |
| `Calculate 2.5 times 4.` | `2.5 * 4` |
| `how much is 9 over 3` | `9 / 3` |
| `10 x 3` | `10 * 3` |
| `please compute 1 + 2` | `1 + 2` |
| `What is 10 minus` | `10 -` (parser will report MissingOperand) |
| `what is the weather` | false |
| `subtract 2 from 10` | false (word order changes are not supported) |
| `ten plus two` | false |
| `` (empty) | false |
| `what is 2+2?` | `2+2` (symbol runs pass through unchanged; the parser normalises them) |
| `what is 10-2*6/4` | `10-2*6/4` |
| `what is -5 plus 3` | `-5 + 3` |
| `what is 2 × 3` | `2 × 3` |
| `What’s 3 plus 4` (typographic apostrophe) | `3 + 4` |
| `what is 2 plus 3\n` (trailing newline) and NBSP between words | `2 + 3` |
| `what is 1.2.3 plus 1` | `1.2.3 + 1` (the parser reports the InvalidNumber with its position) |

Any token made only of digits, `.` and operator symbols (including the aliases) is passed through
untouched; the translator does not check whether it is a valid number. Words are separated by any
whitespace. Only one trailing `?` or `.` is removed.

## F4 — Property and differential tests (R3, R4, R7) · tests only

- **Differential**: 2,000 random valid expressions (fixed seed, 1–12 operands, integers 0–999 and
  one-decimal numbers, all four operators, optional unary minus) evaluated by the engine and by an
  independent reference evaluator written in the test project with a different algorithm
  (shunting-yard to RPN). Values must match exactly. Division by zero must be reported by both.
- **Whitespace invariance**: inserting random spaces between tokens never changes the result.
- **Canonical round-trip**: `Parse(e.ToString())` has the same canonical text and value as `e`.
- On failure the test prints the seed and the expression.

## F5 — CLI one-shot and batch modes (R1, R5, R6) · `calc` binary

- One-shot: all arguments are joined with spaces and evaluated. An argument starting with `-` followed
  by a digit or `.` is part of the expression (`calc -5+3` → `-2`).
- Batch: when stdin is redirected and no expression argument is given, each line is evaluated; blank
  lines and lines starting with `#` are skipped.
- Results go to stdout, one per line. Diagnostics go to stderr.
- Input containing letters is first translated with `NaturalLanguage`; on success stderr shows
  `interpreted as: <canonical>`.
- Exit codes: `0` all succeeded · `1` at least one expression failed · `2` usage error.

| Invocation | stdout | stderr | Exit |
|------------|--------|--------|------|
| `calc "10 - 2 * 6 / 4"` | `7` | | 0 |
| `calc 10 - 2 / 4` | `9.5` | | 0 |
| `calc -5+3` | `-2` | | 0 |
| `calc "What is 10 minus 2 multiplied by 6 divided by 4?"` | `7` | `interpreted as: 10 - 2 * 6 / 4` | 0 |
| `calc "10 + * 3"` | | see error format | 1 |
| `printf '1+2\n\n# c\n10/0\n' \| calc` | `3` | `line 4: ` + error format | 1 |
| `calc --bogus` | | usage text | 2 |

More rules and rows:

- An argument is an option only if it is `-` or `--` followed by a letter (`-e`, `--help`, `--bogus`).
  Everything else is part of the expression: `calc "- 5"` → `-5`, `calc --5` → `5`, `calc 5 --3` → `8`,
  `calc -` → MissingOperand, exit 1.
- The echoed input line is terminal-safe and keeps columns aligned: a tab is shown as a space and a
  control or format character as `?` (one character per character, so the caret stays aligned).
- Batch lines longer than 10,000 characters are rejected as `line N: error[InputTooLong]: …` (exit 1,
  processing continues). A UTF-8 byte-order mark at the start of the first line is ignored.
- When a natural-language question translates but the translation does not parse, stderr shows
  `interpreted as: <translated text>` before the error block (e.g. `What is 10 minus` → `10 -`,
  MissingOperand, caret after `-`).
- With no arguments and stdin not redirected, F5 alone exits 2 with usage (F6 replaces this with the
  interactive mode).

| Invocation | stdout | stderr | Exit |
|------------|--------|--------|------|
| `printf '1\t+\t2 +\n' \| calc` | | caret under end of input, aligned with the echoed spaces | 1 |
| `calc "2 + 3 $(printf '\033')[31m x"` | | echo shows `?` for ESC; no raw control characters on stderr | 1 |
| `printf '\xEF\xBB\xBF1+2\n' \| calc` | `3` | | 0 |
| `calc 1 --x` | | usage | 2 |

Error format (two-space indent, caret under the offending span, `^` repeated `Length` times):
```
error[MissingOperand]: expected a number after '+'
  10 + * 3
       ^
```

## F6 — CLI interactive mode and flags (R1, R6)

- No arguments and stdin not redirected → REPL. Banner line, prompt `> `.
- Commands: `help`, `explain <expression>`, `exit`, `quit`; end of input (Ctrl+D) exits with 0.
- Errors never end the session; the caret is aligned under the text typed after the prompt.
- Flags: `-h`/`--help` (usage to stdout, exit 0), `--version` (`calc 1.0.0`, exit 0),
  `-e`/`--explain` (print each step as `2 * 6 = 12` before the result, any mode).

| Session input | Output contains |
|---------------|-----------------|
| `10-2*6/4` | `7` |
| `explain 10-2*6/4` | `2 * 6 = 12`, `12 / 4 = 3`, `10 - 3 = 7`, `7` |
| `2 +` | `error[MissingOperand]` and the caret line, then the next prompt |
| `help` | the list of commands and operators |
| `exit` | session ends, exit code 0 |

## F7 — REST API (R9) · `Calculator.Api`

| Request | Response |
|---------|----------|
| `POST /v1/evaluations` `{"expression":"10 - 2 * 6 / 4"}` | 200 `{"expression":"10 - 2 * 6 / 4","result":"7"}` |
| same with `"explain":true` | 200 plus `"steps":[{"left":"2","operator":"*","right":"6","result":"12"},…]` |
| `POST /v1/evaluations` `{"expression":"10 / 0"}` | 422 Problem Details: `errorCode` `DivisionByZero`, `position` 3, `length` 1 |
| `POST /v1/evaluations/validate` `{"expression":"10 + * 3"}` | 200 `{"valid":false,"error":{"errorCode":"MissingOperand","position":5,"length":1,"message":…}}` |
| `POST /v1/evaluations/validate` `{"expression":"10-2"}` | 200 `{"valid":true,"canonical":"10 - 2"}` |
| missing or non-string `expression` | 400 Problem Details |
| expression longer than 1,000 characters | 422 `errorCode` `InputTooLong` |
| request body over 16 KB | 413 |
| `GET /health` | 200 |
| `GET /openapi/v1.json` | 200 OpenAPI document |

Results and step values are strings (precision-safe for JavaScript clients).

More rules (verified in the Production environment, not Development):

- Every error response is `application/problem+json` (400, 413, 422 and any unexpected 500) with
  `status`, `title`, `instance` (`<METHOD> <path>`) and `traceId`.
- `InputTooLong` also carries `position` 1000 and `length` = characters beyond the limit.
- Unknown JSON members are rejected with 400 (`{"expression":"1+1","explian":true}`).
- The OpenAPI document declares 200, 400, 413 and 422 responses for both endpoints.

## F8 — MCP tools (R9) · `/mcp` (Streamable HTTP)

| Tool | Input | Output |
|------|-------|--------|
| `evaluate_expression` | `expression` (string) | structured content `{expression, result, steps}` (the text block carries the same JSON, so it contains `7`); errors are MCP tool errors (`isError`) whose text names the error code and position |
| `validate_expression` | `expression` | `{valid, canonical?, error?}` |

Tool descriptions tell the host model to translate the user's words into an expression using only
`+ - * /` and to never compute the answer itself; unsupported questions (grouping, powers, percentages)
must be reported as unsupported, never rearranged or partly computed by the model.

More rows:

| Call | Result |
|------|--------|
| `evaluate_expression` / `validate_expression` with 1,001 characters | tool error text contains `InputTooLong` and `index 1000` |
| either tool with `"expression": null` | tool error text contains `MissingExpression` (no generic error) |
| `evaluate_expression` `10 + * 3` | tool error text contains `MissingOperand`, `index 5` and `length 1` (positions are zero-based) |

Security: the PoC has no authentication and is meant for local use; requests whose `Host` is not
`localhost`/`127.0.0.1`/`[::1]` are rejected (DNS-rebinding protection required by the MCP
Streamable HTTP transport). Deployments override the allowed hosts through configuration.

## F9 — Natural-language endpoint with validated LLM fallback (R9)

`POST /v1/evaluations/natural-language` `{"question": "..."}`

1. Try `NaturalLanguage.TryTranslate`. On success `interpreter` is `rules`.
2. Otherwise, if an LLM is configured (`Llm:Endpoint`, OpenAI-compatible, e.g. local Ollama), ask it
   for `{"expression": "..."}`; `interpreter` is `llm`.
3. The candidate must be ≤ 1,000 characters and pass `Expression.Parse`; only then is it evaluated.

| Case | Response |
|------|----------|
| `What is 10 minus 2 multiplied by 6 divided by 4?` | 200 `{"interpretedExpression":"10 - 2 * 6 / 4","result":"7","interpreter":"rules"}` |
| rules fail, no LLM configured | 422 `errorCode` `Uninterpretable` with a hint |
| fake LLM returns `{"expression":"3 * 4"}` | 200, `interpreter` `llm`, result `12` |
| fake LLM returns prompt-injection text, C# code, SQL, an oversized string, non-JSON, or an empty expression | 422 `Uninterpretable` or the parser's error; nothing is evaluated |
| more than 30 requests per minute from one client | 429 |
