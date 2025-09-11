using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SUI.Client.Core;

public static class PdfReportGenerator
{
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
                            header.Cell().Text("Category").Bold();
                            header.Cell().Text("Value").Bold();
                        });



                        for (int i = 0; i < categories.Length; i++)
                        {
                            table.Cell().Text(categories[i]);
                            table.Cell().Text(values[i].ToString());
                        }
                    });

                    // Generate and embed the bar chart SVG
                    string barChartSvg = GenerateBarChart(categories, values);
                    col.Item().Svg(barChartSvg);

                    // Generate and embed the pie chart SVG
                    string pieChartSvg = GeneratePieChart(categories, values);
                    col.Item().Svg(pieChartSvg);
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            });
        }).GeneratePdf(filePath);
        return filePath;
    }

    private static string GenerateBarChart(string[] categories, double[] values)
    {
        var model = new PlotModel { Title = "Bar Chart", Padding = new OxyThickness(30, 30, 30, 30) };
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

        return ExportPlotToSvg(model);
    }

    private static string GeneratePieChart(string[] categories, double[] values)
    {
        var model = new PlotModel { Title = "Pie Chart", Padding = new OxyThickness(0, 30, 0, 30) };
        var pieSeries = new PieSeries { InsideLabelFormat = "{1}: {0}", OutsideLabelFormat = "", StrokeThickness = 2.0, Diameter = 0.9 };

        var p = categories.Zip(values).Where(x => x.Second > 0).ToArray();
        var c = p.Select(x => x.First).ToArray();
        var v = p.Select(x => x.Second).ToArray();

        for (int i = 0; i < v.Length; i++)
        {
            pieSeries.Slices.Add(new PieSlice(c[i], v[i]));
        }

        model.Series.Add(pieSeries);

        return ExportPlotToSvg(model);
    }


    private static string ExportPlotToSvg(PlotModel model)
    {
        using var stream = new MemoryStream();
        var exporter = new SvgExporter { Width = 600, Height = 400 };
        exporter.Export(model, stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}