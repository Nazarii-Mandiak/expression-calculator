# Expression Calculator

An exact command-line calculator for .NET 10: type an expression and get the correct answer, with
standard operator precedence and no floating-point surprises.

```console
$ calc "10 - 2 * 6 / 4"
7
```

## Highlights

- **Correct by construction.** `*` and `/` bind tighter than `+` and `-`, and equal operators run left to
  right, so `10 - 2 * 6 / 4` is `7`.
- **Exact decimals.** Arithmetic uses `System.Decimal`, so `0.1 + 0.2` is `0.3` on every machine and in
  every language setting.
- **Errors that point at the problem.** Every error names a code and underlines the exact position in
  your input.
- **Four ways to use it.** One-shot commands, an interactive session, piped scripts, and step-by-step
  explanations.
- **Plain-English questions.** `calc "What is 10 minus 2 multiplied by 6 divided by 4?"` prints `7`.

The same engine also runs as a small REST and MCP service; see
[Service proof of concept](#service-proof-of-concept).

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (any 10.0 version).

```bash
git clone https://github.com/Nazarii-Mandiak/expression-calculator.git
cd expression-calculator
dotnet test                                            # builds everything and runs 385 tests
dotnet publish src/Calculator.Cli -c Release -o out/calc
```

Then run the calculator:

| Shell | Command |
| ----- | ------- |
| macOS / Linux | `./out/calc/calc "10 - 2 * 6 / 4"` |
| Windows PowerShell | `.\out\calc\calc.exe "10 - 2 * 6 / 4"` |
| Any (no publish step) | `dotnet run --project src/Calculator.Cli -- "10 - 2 * 6 / 4"` |

The examples below write `calc` for whichever command you use.

## Using calc

**One-shot.** Pass the expression as an argument. Quote it if it contains `*`, so the shell does not
expand it into file names.

```console
$ calc "0.1 + 0.2"
0.3
```

**Interactive.** Run `calc` with no arguments. Type expressions, `explain <expression>`, `help`, or
`exit`. A mistake never ends the session.

```text
calc 1.0.0 - type an expression, 'help' or 'exit'
> 10-2*6/4
7
> explain 2+3*4
3 * 4 = 12
2 + 12 = 14
14
> exit
```

**Piped.** Send one expression per line. Blank lines and lines starting with `#` are skipped, and a
failing line is reported without stopping the rest.

```console
$ printf '1+2\n# comment\n10/0\n0.1+0.2\n' | calc
3
line 3: error[DivisionByZero]: division by zero
  10/0
    ^
0.3
```

**Step by step.** Add `-e` (or `--explain`) to see each operation in the order it is performed.

```console
$ calc -e "10 - 2 * 6 / 4"
2 * 6 = 12
12 / 4 = 3
10 - 3 = 7
7
```

**Questions.** Simple English questions are translated into an expression first. The translation is
printed on stderr, so scripts still receive only the result.

```console
$ calc "What is 10 minus 2 multiplied by 6 divided by 4?"
interpreted as: 10 - 2 * 6 / 4
7
```

**Errors.** Errors go to stderr with a code, a message and carets under the problem. The exit code is
`1` when an expression fails and `2` for an unknown option.

```console
$ calc "10 + * 3"
error[MissingOperand]: expected a number after '+'
  10 + * 3
       ^
```

The [manual](MANUAL.txt) covers every option, the full syntax, precision rules and all error codes.

## Service proof of concept

`Calculator.Api` shows how the engine becomes a shared service. It runs locally and needs no paid
API keys.

```bash
dotnet run --project src/Calculator.Api        # listens on http://localhost:5080
```

| Endpoint | Purpose |
| -------- | ------- |
| `POST /v1/evaluations` | Evaluate `{"expression": "...", "explain": false}` |
| `POST /v1/evaluations/validate` | Check an expression without evaluating it |
| `POST /v1/evaluations/natural-language` | Answer `{"question": "..."}`; limited to 30 requests per minute |
| `GET /health` | Liveness check |
| `GET /openapi/v1.json` | OpenAPI description of the endpoints |
| `/mcp` | MCP server with the tools `evaluate_expression` and `validate_expression` |

```http
POST /v1/evaluations/natural-language
{"question": "What is 10 minus 2 multiplied by 6 divided by 4?"}

200 OK
{"interpretedExpression": "10 - 2 * 6 / 4", "result": "7", "interpreter": "rules"}
```

Errors use the standard RFC 9457 Problem Details format and include the error code and position.
Ready-made requests are in [Calculator.Api.http](src/Calculator.Api/Calculator.Api.http).

**MCP.** AI assistants can call the calculator as a tool. For example, in Claude Code:

```bash
claude mcp add --transport http calculator http://localhost:5080/mcp
```

**Optional language model.** Questions the built-in rules cannot translate return `422 Uninterpretable`.
To try a model for those, point the service at any OpenAI-compatible server, such as a local Ollama:

```bash
export Llm__Endpoint=http://localhost:11434/v1
export Llm__Model=<model-name>
dotnet run --project src/Calculator.Api
```

The model only proposes expression text. The engine parses and checks that text like any other input
before evaluating it, so model output is never executed. The proof of concept has no authentication
and accepts requests for `localhost` only.

## How it works

```text
   calc (console)      REST  /v1/*        MCP  /mcp
         │                  │                 │
         └──────────────────┼─────────────────┘
                            ▼
                     Calculator.Core
     text ─► Expression.Parse ─► Evaluate ─► result + steps
                (no I/O, no dependencies)
```

One small engine, `Calculator.Core`, does all the work. It has no I/O and no third-party
dependencies, and the console app and the service are thin adapters around it. That is why every
channel gives identical results and errors.

The engine reads the whole input before calculating anything. Invalid input is therefore always
reported at its leftmost problem, and no half-evaluated result is ever produced.

Plain-English questions, whether translated by rules or by a language model, become ordinary
expression text that goes through the same strict parser as typed input.

## Quality

- **385 automated tests**: 163 for the engine, 79 for the console app and 143 for the service.
- **An independent check**: 2,000 random expressions are evaluated by the engine and by a separately
  written reference evaluator, and the results must match exactly.
- **Mutation testing**: Stryker.NET injects small bugs into the engine, and the tests catch 97.44% of
  them.
- **A strict build**: compiler warnings are errors, code style is enforced, and vulnerable packages
  fail the build.
- **Continuous integration**: `scripts/verify.sh --full` runs on Ubuntu and Windows for every change.

## Documentation

| Document | What it covers |
| -------- | -------------- |
| [MANUAL.txt](MANUAL.txt) | User manual for `calc`: modes, syntax, precision, errors |
| [docs/DESIGN.md](docs/DESIGN.md) ([PDF](docs/DESIGN.pdf)) | Evolving the calculator into a service for many products, including AI input |
| [docs/AI_USAGE.md](docs/AI_USAGE.md) | AI tools used, what they generated, what I refined, and how correctness was verified |
| [docs/SPEC.md](docs/SPEC.md) | Requirements and acceptance criteria that the tests encode |
| [docs/adr/](docs/adr/README.md) | Architecture decision records: the main design choices and their trade-offs |

## Troubleshooting

- **"A compatible .NET SDK was not found".** Install any .NET 10 SDK. CI pins 10.0.302 because
  `scripts/verify.sh --full` restores packages in locked mode; `dotnet build` and `dotnet test` work
  with any 10.0 SDK.
- **A build error mentions a package advisory (NU1901–NU1904) or a new analyzer warning.** Warnings are
  errors in this repository. A newer SDK, or a security advisory published after this release, can
  therefore stop the build without any code change. To build anyway, run
  `dotnet test -p:TreatWarningsAsErrors=false`.

## AI-assisted development

This project was built with Claude Code inside a workflow with enforced guardrails: tests were written
and approved before the code, separate agents wrote tests and code, and every change was reviewed
before it was merged. [docs/AI_USAGE.md](docs/AI_USAGE.md) describes exactly what was generated, what I
decided and changed, and how the result was verified.
