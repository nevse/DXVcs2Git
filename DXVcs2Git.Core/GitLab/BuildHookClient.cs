using System.Runtime.Serialization;
using GitLab.NET;

namespace DXVcs2Git.Core.GitLab {
    [DataContract]
    public class BuildHookClient : ProjectHookClient {
        [DataMember(Name = "build_id")]
        public int BuildId;
        [DataMember(Name = "build_name")]
        public string BuildName;
        [DataMember(Name = "build_status")]
        public BuildStatus Status;
        [DataMember(Name = "commit")]
        public BuildHookCommit Commit;
        [DataMember(Name = "project_id")]
        public int ProjectId;
        [DataMember(Name = "project_name")]
        public string ProjectName;
        [DataMember(Name = "ref")]
        public string Branch;
    }
    [DataContract]
    public class BuildHookCommit {
        [DataMember(Name = "sha")]
        public string Id;
    }
}
