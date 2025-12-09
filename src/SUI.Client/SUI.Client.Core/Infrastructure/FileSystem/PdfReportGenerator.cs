using System.Globalization;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public static class PdfReportGenerator
{
    private const string Category = "Category";
    private const string Value = "Value";

    public static string GenerateReport(string filePath, string title, string[] categories, double[] values)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Text(title).Bold().FontSize(20).AlignCenter();

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(200);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(Category).Bold();
                            header.Cell().Text(Value).Bold();
                        });

                        for (int i = 0; i < categories.Length; i++)
                        {
                            table.Cell().Text(categories[i]);
                            table.Cell().Text(values[i].ToString(CultureInfo.InvariantCulture));
                        }
                    });

                    // Generate and embed the bar chart SVG
                    string barChartSvg = GenerateBarChart("Matching Results", categories, values);
                    col.Item().Svg(barChartSvg);

                    // Generate and embed the pie chart SVG
                    string pieChartSvg = GeneratePieChart("Matching Results", categories, values);
                    col.Item().Svg(pieChartSvg);
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            });
        }).GeneratePdf(filePath);
        return filePath;
    }

    public static string GenerateReconciliationReport(string filePath, string title, int totalRecords,
        Tuple<string[], double[]> main, Tuple<string[], double[]> differences,
        Tuple<string[], double[]> matching)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Header().Text(title).Bold().FontSize(20).AlignCenter();
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(200);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(Category).Bold();
                            header.Cell().Text(Value).Bold();
                        });
                        table.Cell().Text("Total Records Processed");
                        table.Cell().Text(totalRecords.ToString());
                        for (int i = 0; i < main.Item1.Length; i++)
                        {
                            table.Cell().Text(main.Item1[i]);
                            table.Cell().Text(main.Item2[i].ToString(CultureInfo.InvariantCulture));
                        }
                    });

                    string barChartSvg = GenerateBarChart("Main", main.Item1, main.Item2);
                    col.Item().Svg(barChartSvg);

                    string pieChartSvg = GeneratePieChart("Main", main.Item1, main.Item2);
                    col.Item().Svg(pieChartSvg);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(200);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(Category).Bold();
                            header.Cell().Text(Value).Bold();
                        });
                        for (int i = 0; i < differences.Item1.Length; i++)
                        {
                            table.Cell().Text(differences.Item1[i]);
                            table.Cell().Text(differences.Item2[i].ToString(CultureInfo.InvariantCulture));
                        }
                    });
                    string barChart2Svg = GenerateBarChart("Differences", differences.Item1, differences.Item2, 630, 630);
                    col.Item().Svg(barChart2Svg);

                    string pieChart2Svg = GeneratePieChart("Differences", differences.Item1, differences.Item2);
                    col.Item().Svg(pieChart2Svg);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(200);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text(Category).Bold();
                            header.Cell().Text(Value).Bold();
                        });
                        for (int i = 0; i < matching.Item1.Length; i++)
                        {
                            table.Cell().Text(matching.Item1[i]);
                            table.Cell().Text(matching.Item2[i].ToString(CultureInfo.InvariantCulture));
                        }
                    });
                    string barChart3Svg = GenerateBarChart("Matching", matching.Item1, matching.Item2);
                    col.Item().Svg(barChart3Svg);

                    string pieChart3Svg = GeneratePieChart("Matching", matching.Item1, matching.Item2);
                    col.Item().Svg(pieChart3Svg);
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            });
        }).GeneratePdf(filePath);
        return filePath;
    }

    private static string GenerateBarChart(string title, string[] categories, double[] values, int width = 600, int height = 400)
    {
        var model = new PlotModel { Title = title, Padding = new OxyThickness(30, 30, 30, 30) };
        var barSeries = new BarSeries { LabelPlacement = LabelPlacement.Inside, LabelFormatString = "{0}" };

        for (int i = 0; i < values.Length; i++)
        {
            barSeries.Items.Add(new BarItem { Value = values[i] });
        }

        model.Series.Add(barSeries);
        model.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Left,
            ItemsSource = categories,
            IsTickCentered = true,
        });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Count" });

        return ExportPlotToSvg(model, width, height);
    }

    private static string GeneratePieChart(string title, string[] categories, double[] values, int width = 600, int height = 400)
    {
        var model = new PlotModel { Title = title, Padding = new OxyThickness(0, 30, 0, 30) };
        var pieSeries = new PieSeries { InsideLabelFormat = "{0} ({2:0}%)", OutsideLabelFormat = "{1}", StrokeThickness = 2.0, Diameter = 0.9 };

        var p = categories.Zip(values).Where(x => x.Second > 0).ToArray();
        var c = p.Select(x => x.First).ToArray();
        var v = p.Select(x => x.Second).ToArray();

        for (int i = 0; i < v.Length; i++)
        {
            pieSeries.Slices.Add(new PieSlice(c[i], v[i]));
        }

        model.Series.Add(pieSeries);

        return ExportPlotToSvg(model, width, height);
    }

    private static string ExportPlotToSvg(PlotModel model, int width = 600, int height = 400)
    {
        using var stream = new MemoryStream();
        var exporter = new SvgExporter { Width = width, Height = height };
        exporter.Export(model, stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}