using System.Text;

using Calculator.Cli;

// Composition root: the only place that touches the real console.
try
{
    Console.InputEncoding = Encoding.UTF8;
    Console.OutputEncoding = Encoding.UTF8;
}
catch (IOException)
{
    // Windows without an attached console cannot change the encoding; keep the default.
}

return new CliApp(Console.In, Console.Out, Console.Error, Console.IsInputRedirected).Run(args);
