﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using DXVcs2Git.DXVcs;
using DXVCS;
using NUnit.Framework;

namespace DXVcs2Git.Tests {
    [TestFixture]
    public class DXVcsServiceTests {
        DXVcsConfig defaultConfig = new DXVcsConfig() { AuxPath = @"net.tcp://vcsservice.devexpress.devx:9091/DXVCSService" };

        [Test]
        public void SimpleStart() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            Assert.IsNotNull(repo);
        }
        [Test]
        public void GetProjectHistoryFromTestHistory() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistory", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            Assert.AreEqual(3, history.Count());
            Assert.AreEqual(@"9/9/2015 7:30:57 PM,,,Create,,Project,Litvinov,1", FormatProjectHistoryItem(history[0]));
            Assert.AreEqual(@"9/9/2015 7:31:09 PM,,,Shared from $/Sandbox/litvinov/DXVcsTest/test.txt,test.txt,File,Litvinov,2", FormatProjectHistoryItem(history[1]));
            Assert.AreEqual(@"9/9/2015 7:05:37 PM,,,Checked in (2),test.txt,File,Litvinov,2", FormatProjectHistoryItem(history[2]));
        }
        string FormatProjectHistoryItem(ProjectHistoryInfo historyItem) {
            return $"{historyItem.ActionDate},{historyItem.Comment},{historyItem.Label},{historyItem.Message},{historyItem.Name},{historyItem.Type},{historyItem.User},{historyItem.Version}";
        }
        [Test]
        public void GroupHistoryByTimeStampInFolderIfAddingOneFile() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistory", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            var grouped = history.GroupBy(x => x.ActionDate).ToList();
            Assert.AreEqual(3, grouped.Count);
        }
        [Test]
        public void GroupHistoryByTimeStampInFolderIfAddingTwoFiles() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            var history = repo.GetProjectHistory(@"$/Sandbox/litvinov/DXVcsTest/testhistorybyaddingtwofiles", true, new DateTime(2015, 9, 9), new DateTime(2015, 9, 10));
            var grouped = history.GroupBy(x => x.ActionDate).ToList();
            Assert.AreEqual(4, grouped.Count);
            Assert.AreEqual(@"9/9/2015 7:45:26 PM,,,Create,,Project,Litvinov,1", FormatProjectHistoryItem(grouped[0].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:46:32 PM,1,,Created,1.txt,File,Litvinov,2", FormatProjectHistoryItem(grouped[1].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:46:32 PM,1,,Created,2.txt,File,Litvinov,3", FormatProjectHistoryItem(grouped[2].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:47:22 PM,1,,Checked in (2),1.txt,File,Litvinov,2", FormatProjectHistoryItem(grouped[3].ToList()[0]));
            Assert.AreEqual(@"9/9/2015 7:47:22 PM,1,,Checked in (2),2.txt,File,Litvinov,2", FormatProjectHistoryItem(grouped[3].ToList()[1]));
        }
        [Test]
        public void GetProjectForTimeStamp() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
        }
        [Test, Explicit]
        public void GetProjectHistoryForXpfCore152() {
            string path = @"c:\test\";
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            List<string> branches = new List<string>() {
                @"$/NET.OLD/2009.1/WPF/DevExpress.Wpf.Core",
                @"$/NET.OLD/2009.2/WPF/DevExpress.Wpf.Core",
                @"$/NET.OLD/2009.3/WPF/DevExpress.Wpf.Core",
                @"$/NET.OLD/2010.1/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2010.2/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2011.1/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2011.2/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2012.1/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2012.2/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2013.1/XPF/DevExpress.Xpf.Core",
                @"$/NET.OLD/2013.2/XPF/DevExpress.Xpf.Core",
                @"$/2014.1/XPF/DevExpress.Xpf.Core",
                @"$/2014.2/XPF/DevExpress.Xpf.Core",
                @"$/2015.1/XPF/DevExpress.Xpf.Core",
                @"$/2015.2/XPF/DevExpress.Xpf.Core",
            };
            List<DateTime> branchesCreatedTime = branches.Select(x => {
                var history = repo.GetProjectHistory(x, true);
                return history.First(IsBranchCreatedTimeStamp).ActionDate;
            }).ToList();
            DateTime previous = DateTime.MinValue;
            var resultHistory = Enumerable.Empty<HistoryItem>();
            for (int i = 0; i < branchesCreatedTime.Count - 1; i++) {
                DateTime currentStamp = branchesCreatedTime[i];
                string branch = branches[i];
                var history = repo.GetProjectHistory(branch, true, previous, currentStamp);
                var projectHistory = CalcProjectHistory(history);
                foreach (var historyItem in projectHistory) {
                    historyItem.Branch = branch;
                }
                resultHistory = resultHistory.Concat(projectHistory);
            }
        }
    //    CloneOptions options = new CloneOptions();
    //    var credentials = new UsernamePasswordCredentials();
    //    credentials.Username = Constants.Identity.Name;
    //        credentials.Password = "q1w2e3r4t5y6";
    //        options.CredentialsProvider += (url, fromUrl, types) => credentials;

    //        string clonedRepoPath = Repository.Clone(testUrl, scd.DirectoryPath, options);
    //    string file = Path.Combine(scd.DirectoryPath, "testpush.txt");
    //    var rbg = new RandomBufferGenerator(30000);
    //        using (var repo = new Repository(clonedRepoPath)) {
    //            for (int i = 0; i< 1; i++) {
    //                var network = repo.Network.Remotes.First();
    //FetchOptions fetchOptions = new FetchOptions();
    //fetchOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
    //                repo.Fetch(network.Name, fetchOptions);

    //                File.WriteAllBytes(file, rbg.GenerateBufferFromSeed(30000));
    //                repo.Stage(file);
    //                Signature author = Constants.Signature;

    //Commit commit = repo.Commit($"Here's a commit {i + 1} i made!", author, author);
    //PushOptions pushOptions = new PushOptions();
    //pushOptions.CredentialsProvider += (url, fromUrl, types) => credentials;
    //                repo.Network.Push(repo.Branches["master"], pushOptions);
    //            }
    //        }

        [Test]
        public void TestFindCreateBranchTimeStamp() {
            var repo = DXVcsConectionHelper.Connect(defaultConfig.AuxPath);
            var vcsPath = @"$/2014.1/XPF/DevExpress.Xpf.Core/DevExpress.Xpf.Core";
            var history = repo.GetProjectHistory(vcsPath, true);
            var create = history.Where(IsBranchCreatedTimeStamp).ToList();
            Assert.AreEqual(1, create.Count);
            Assert.AreEqual(635187859620700000, create[0].ActionDate.Ticks);
            //var projectHistory = history.Reverse().GroupBy(x => x.ActionDate).OrderBy(x => x.First().ActionDate).Select(x => new HistoryItem(x.First().ActionDate, x.ToList()));
            //var project = projectHistory.Where(x => x.History.Any(h => h.Message != null && h.Message.ToLowerInvariant().Contains("branch"))).ToList();
        }
        IEnumerable<HistoryItem> CalcProjectHistory(IEnumerable<ProjectHistoryInfo> history) {
            return history.Reverse().GroupBy(x => x.ActionDate).OrderBy(x => x.First().ActionDate).Select(x => new HistoryItem(x.First().ActionDate, x.ToList()));
        }

        static bool IsBranchCreatedTimeStamp(ProjectHistoryInfo x) {
            return x.Message != null && x.Message.ToLowerInvariant() == "create";
        }
    }

    public class HistoryItem {
        public string Branch { get; set; }
        public DateTime TimeStamp { get; private set; }
        public IList<ProjectHistoryInfo> History { get; private set; }

        public HistoryItem(DateTime timeStamp, IList<ProjectHistoryInfo> info) {
            TimeStamp = timeStamp;
            History = info;
        }
    }
}
