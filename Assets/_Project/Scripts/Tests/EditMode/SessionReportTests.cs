using LotoSim.Core;
using LotoSim.Evaluation;
using NUnit.Framework;

namespace LotoSim.Tests
{
    public class SessionReportTests
    {
        private static MistakeRecord Record(MistakeSeverity severity, int step = 1) =>
            new(new MistakeDefinition($"M{step}{severity}", severity, "t", "e"), step, 0f);

        [Test]
        public void Score_NoMistakes_Is100AndPassed()
        {
            var report = new SessionReport(GameMode.Guided, 300f, new MistakeRecord[0]);
            Assert.AreEqual(100, report.Score);
            Assert.IsTrue(report.Passed);
        }

        [Test]
        public void Score_MinorAndCritical_SubtractsPenalties()
        {
            var report = new SessionReport(GameMode.Assessment, 300f,
                new[] { Record(MistakeSeverity.Minor), Record(MistakeSeverity.Critical, 3) });
            Assert.AreEqual(100 - SessionReport.MinorPenalty - SessionReport.CriticalPenalty, report.Score);
            Assert.AreEqual(1, report.CriticalCount);
        }

        [Test]
        public void Passed_AnyCritical_FailsEvenWithHighScore()
        {
            var report = new SessionReport(GameMode.Assessment, 300f, new[] { Record(MistakeSeverity.Critical) });
            Assert.GreaterOrEqual(report.Score, SessionReport.PassingScore);
            Assert.IsFalse(report.Passed);
        }

        [Test]
        public void Passed_ManyMinors_FailsBelowPassingScore()
        {
            var records = new MistakeRecord[7];
            for (int i = 0; i < records.Length; i++)
                records[i] = Record(MistakeSeverity.Minor, i);
            var report = new SessionReport(GameMode.Assessment, 300f, records);
            Assert.Less(report.Score, SessionReport.PassingScore);
            Assert.IsFalse(report.Passed);
        }

        [Test]
        public void Score_NeverNegative()
        {
            var records = new MistakeRecord[8];
            for (int i = 0; i < records.Length; i++)
                records[i] = Record(MistakeSeverity.Critical, i);
            Assert.AreEqual(0, new SessionReport(GameMode.Assessment, 0f, records).Score);
        }
    }
}
