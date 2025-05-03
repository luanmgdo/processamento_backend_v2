using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using HtmlAgilityPack;

class ProcessamentoBackend
{
    static async Task Main()
    {
        // Timer geral
        Stopwatch timerTotal = new Stopwatch();
        timerTotal.Start();

        // Ao executar o projeto usando 'dotnet run', o diretorio base reconhecido pelo código é '\bin\Debug\net9.0\' portanto para acessar a 'base real' deve se pegar a partir dos diretorios 'pais'
        string basePath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;

        string inputDirectory = Path.Combine(basePath, "Documentos");
        string outputDirectory = Path.Combine(basePath, "Saida");

        string relatorioPath = Path.Combine(basePath, "Relatorio final.txt");

        int qtdSuccessPDF = 0;
        int qtdSuccessHTML = 0;
        int qtdError = 0;
        double tempoPdf = 0;
        double tempoHtml = 0;

        Directory.CreateDirectory(outputDirectory);

        // Arquivos PDF e HTML
        string[] archives = Directory.GetFiles(inputDirectory)
            .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (archives.Length == 0)
        {
            Console.WriteLine("Nenhum arquivo PDF ou HTML encontrado.");
            return;
        }

        // criar task para processar arquivos paralelamente
        var tasks = archives.Select(async path =>
        {
            await Task.Run(() =>
            {
                Stopwatch timerArch = Stopwatch.StartNew();

                string filenameWExt = Path.GetFileNameWithoutExtension(path);
                string pathTxt = Path.Combine(outputDirectory, filenameWExt + ".txt");

                try
                {
                    string extensao = Path.GetExtension(path).ToLower();

                    if (extensao == ".pdf")
                    {
                        using (PdfDocument doc = PdfDocument.Open(path))
                        using (StreamWriter writer = new StreamWriter(pathTxt, false))
                        {
                            foreach (Page pagina in doc.GetPages())
                            {
                                writer.WriteLine(pagina.Text);
                            }
                        }

                        qtdSuccessPDF++;

                        timerArch.Stop();
                        tempoPdf += timerArch.Elapsed.TotalSeconds;
                    }
                    else if (extensao == ".html")
                    {
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.Load(path);

                        string texto = htmlDoc.DocumentNode.InnerText;

                        File.WriteAllText(pathTxt, texto);

                        qtdSuccessHTML++;

                        timerArch.Stop();
                        tempoHtml += timerArch.Elapsed.TotalSeconds;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"error: {ex}");
                    qtdError++;
                }
            });
        });

        // Espera todas as tarefas terminarem
        await Task.WhenAll(tasks);

        // Parar cronometro geral
        timerTotal.Stop();

        // Relatório final
        using (StreamWriter relatorioFinal = new StreamWriter(relatorioPath, false))
        {
            relatorioFinal.WriteLine($"Arquivos processados com sucesso: {qtdSuccessHTML + qtdSuccessPDF}");
            relatorioFinal.WriteLine($"Arquivos com erro: {qtdError}");
            relatorioFinal.WriteLine($"Tempo médio de processamento dos PDF's: {tempoPdf / qtdSuccessPDF:F3} segundos");
            relatorioFinal.WriteLine($"Tempo médio de processamento dos HTML's: {tempoHtml / qtdSuccessHTML:F3} segundos");
            relatorioFinal.WriteLine($"Tempo de processamento total: {timerTotal.Elapsed.TotalSeconds:F2} segundos");
        }

    }
}
