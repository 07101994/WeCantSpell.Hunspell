using System.IO;
using BenchmarkDotNet.Attributes;
using WeCantSpell.Hunspell.Benchmarking.MicroSuites.Infrastructure;

namespace WeCantSpell.Hunspell.Benchmarking.MicroSuites
{
    [SimpleJob(launchCount: 1, warmupCount: 2, targetCount: 3), RankColumn, MemoryDiagnoser]
    public class EnWordListLoadSuite
    {
        [Benchmark(Description = "en-US load performance", Baseline = true)]
        public void LoadEnUs()
        {
            var wordList = WordList.CreateFromFiles(Path.Combine(DataFilePaths.TestFilesFolderPath, "English (American).dic"));
        }

        [Benchmark(Description = "en-AU load performance")]
        public void LoadEnAu()
        {
            var wordList = WordList.CreateFromFiles(Path.Combine(DataFilePaths.TestFilesFolderPath, "English (Australian).dic"));
        }

        [Benchmark(Description = "en-GB load performance")]
        public void LoadEnGb()
        {
            var wordList = WordList.CreateFromFiles(Path.Combine(DataFilePaths.TestFilesFolderPath, "English (British).dic"));
        }

        [Benchmark(Description = "en-CA load performance")]
        public void LoadEnCa()
        {
            var wordList = WordList.CreateFromFiles(Path.Combine(DataFilePaths.TestFilesFolderPath, "English (Canadian).dic"));
        }

        [Benchmark(Description = "en-ZA load performance")]
        public void LoadEnZa()
        {
            var wordList = WordList.CreateFromFiles(Path.Combine(DataFilePaths.TestFilesFolderPath, "English (South African).dic"));
        }
    }
}
