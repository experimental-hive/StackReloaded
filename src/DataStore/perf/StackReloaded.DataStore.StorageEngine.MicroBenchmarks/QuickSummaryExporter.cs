using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using MarkdownHighlightStrategy = BenchmarkDotNet.Exporters.MarkdownExporter.MarkdownHighlightStrategy;

namespace StackReloaded.DataStore.StorageEngine.MicroBenchmarks
{
    internal class QuickSummaryExporter : ExporterBase
    {
        protected override string FileExtension => "md";
#pragma warning disable CA1308 // Normalize strings to uppercase
        protected override string FileNameSuffix => $"-{this.Dialect.ToLowerInvariant()}";
#pragma warning restore CA1308 // Normalize strings to uppercase

        private string Dialect { get; set; }

        public static readonly IExporter Default = new QuickSummaryExporter
        {
            Dialect = nameof(Default),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold
        };

        public static readonly IExporter Console = new QuickSummaryExporter
        {
            Dialect = nameof(Console),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.None
        };

        public static readonly IExporter StackOverflow = new QuickSummaryExporter
        {
            Dialect = nameof(StackOverflow),
            Prefix = "    ",
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold
        };

        public static readonly IExporter GitHub = new QuickSummaryExporter
        {
            Dialect = nameof(GitHub),
            UseCodeBlocks = true,
            CodeBlockStart = "``` ini",
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold,
            ColumnsStartWithSeparator = true,
            EscapeHtml = true
        };

        public static readonly IExporter Atlassian = new QuickSummaryExporter
        {
            Dialect = nameof(Atlassian),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Bold,
            TableHeaderSeparator = " ||",
            UseHeaderSeparatingRow = false,
            ColumnsStartWithSeparator = true,
            UseCodeBlocks = true,
            CodeBlockStart = "{noformat}",
            CodeBlockEnd = "{noformat}",
            BoldMarkupFormat = "*{0}*"
        };

        // Only for unit tests
        internal static readonly IExporter Mock = new QuickSummaryExporter
        {
            Dialect = nameof(Mock),
            StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.Marker
        };

        protected string Prefix = string.Empty;
        protected bool UseCodeBlocks;
        protected string CodeBlockStart = "```";
        protected string CodeBlockEnd = "```";
        protected MarkdownHighlightStrategy StartOfGroupHighlightStrategy = MarkdownHighlightStrategy.None;
        protected string TableHeaderSeparator = " |";
        protected string TableColumnSeparator = " |";
        protected bool UseHeaderSeparatingRow = true;
        protected bool ColumnsStartWithSeparator;
        protected string BoldMarkupFormat = "**{0}**";
        protected bool EscapeHtml;
        
        private QuickSummaryExporter() { }

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            if (this.UseCodeBlocks)
            {
                logger.WriteLine(this.CodeBlockStart);
            }

            logger = GetRightLogger(logger);
            logger.WriteLine();
            //foreach (string infoLine in summary.HostEnvironmentInfo.ToFormattedString())
            //{
            //    logger.WriteLineInfo(infoLine);
            //}

            //logger.WriteLineInfo(summary.AllRuntimes);
            //logger.WriteLine();

            if (summary.BenchmarksCases.Length != 0)
            {
                logger.WriteLineInfo(summary.BenchmarksCases[0].Descriptor.Type.FullName);
                logger.WriteLine();
            }

            PrintTable(summary.Table, logger);

            //// TODO: move this logic to an analyser
            //var benchmarksWithTroubles = summary.Reports.Where(r => !r.GetResultRuns().Any()).Select(r => r.BenchmarkCase).ToList();
            //if (benchmarksWithTroubles.Count > 0)
            //{
            //    logger.WriteLine();
            //    logger.WriteLineError("Benchmarks with issues:");
            //    foreach (var benchmarkWithTroubles in benchmarksWithTroubles)
            //    {
            //        logger.WriteLineError("  " + benchmarkWithTroubles.DisplayInfo);
            //    }
            //}
        }

        private ILogger GetRightLogger(ILogger logger)
        {
            if (string.IsNullOrEmpty(this.Prefix)) // most common scenario!! we don't need expensive LoggerWithPrefix
            {
                return logger;
            }

            return new LoggerWithPrefix(logger, this.Prefix);
        }

        private void PrintTable(SummaryTable table, ILogger logger)
        {
            if (table.FullContent.Length == 0)
            {
                logger.WriteLineError("There are no benchmarks found ");
                logger.WriteLine();
                return;
            }

            //table.PrintCommonColumns(logger);
            //logger.WriteLine();

            if (this.UseCodeBlocks)
            {
                logger.Write(this.CodeBlockEnd);
                logger.WriteLine();
            }

            if (this.ColumnsStartWithSeparator)
            {
                logger.Write(this.TableHeaderSeparator.TrimStart());
            }

            table.PrintLine(table.FullHeader, logger, string.Empty, this.TableHeaderSeparator);
            if (this.UseHeaderSeparatingRow)
            {
                if (this.ColumnsStartWithSeparator)
                {
                    logger.Write(this.TableHeaderSeparator.TrimStart());
                }

                logger.WriteLineStatistic(string.Join("",
                    table.Columns.Where(c => c.NeedToShow).Select(column => new string('-', column.Width) + GetJustificationIndicator(column.Justify) + "|")));
            }

            int rowCounter = 0;
            bool highlightRow = false;
            var separatorLine = Enumerable.Range(0, table.ColumnCount).Select(_ => "").ToArray();
            foreach (var line in table.FullContent)
            {
                if (rowCounter > 0 && table.FullContentStartOfLogicalGroup[rowCounter] && table.SeparateLogicalGroups)
                {
                    // Print logical separator
                    if (this.ColumnsStartWithSeparator)
                        logger.Write(this.TableColumnSeparator.TrimStart());
                    table.PrintLine(separatorLine, logger, string.Empty, this.TableColumnSeparator, highlightRow, false, this.StartOfGroupHighlightStrategy,
                        this.BoldMarkupFormat, false);
                }

                // Each time we hit the start of a new group, alternative the colour (in the console) or display bold in Markdown
                if (table.FullContentStartOfHighlightGroup[rowCounter])
                {
                    highlightRow = !highlightRow;
                }

                if (this.ColumnsStartWithSeparator)
                    logger.Write(this.TableColumnSeparator.TrimStart());

                table.PrintLine(line, logger, string.Empty, this.TableColumnSeparator, highlightRow, table.FullContentStartOfHighlightGroup[rowCounter],
                    this.StartOfGroupHighlightStrategy, this.BoldMarkupFormat, this.EscapeHtml);
                rowCounter++;
            }
        }

        private static string GetJustificationIndicator(SummaryTable.SummaryTableColumn.TextJustification textJustification)
        {
            return textJustification switch
            {
                SummaryTable.SummaryTableColumn.TextJustification.Left => " ",
                SummaryTable.SummaryTableColumn.TextJustification.Right => ":",
                _ => " ",
            };
        }
    }
}
