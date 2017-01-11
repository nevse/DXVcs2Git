using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DXVcs2Git.Core;
using GitLab.NET;
using GitLab.NET.ResponseModels;
using User = GitLab.NET.ResponseModels.User;

namespace DXVcs2Git.Git {
    public class GitLabWrapper {
        const string IgnoreValidation = "[IGNOREVALIDATION]";
        readonly GitLabClient client;
        public GitLabWrapper(string server, string token) {
            client = new GitLabClient(new Uri(server), token);
        }
        public async Task<bool> IsAdmin() {
            var currentUser = await client.Users.GetCurrent();
            return currentUser.Data.IsAdmin ?? false;
        }
        public async Task<IEnumerable<Project>> GetProjects() {
            return (await client.Projects.Accessible()).Data;
        }
        public async Task<IEnumerable<Project>> GetAllProjects() {
            return (await client.Projects.GetAll()).Data;
        }
        public async Task<Project> GetProject(uint id) {
            return (await this.client.Projects.Find(id)).Data;
        }
        public async Task<Project> FindProject(string project) {
            return (await client.Projects.Accessible()).Data.FirstOrDefault(x =>
                string.Compare(x.HttpUrlToRepo, project, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(x.SshUrl, project, StringComparison.InvariantCultureIgnoreCase) == 0);
        }
        public async Task<Project> FindProjectFromAll(string project) {
            return (await GetAllProjects()).FirstOrDefault(x =>
                string.Compare(x.HttpUrlToRepo, project, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(x.SshUrl, project, StringComparison.InvariantCultureIgnoreCase) == 0);
        }
        public async Task<IEnumerable<MergeRequest>> GetMergeRequests(Project project, Func<MergeRequest, bool> mergeRequestsHandler = null) {
            mergeRequestsHandler = mergeRequestsHandler ?? (x => true);
            var mergeRequests = await client.MergeRequests.GetAll(project.Id, MergeRequestState.Opened);
            return mergeRequests.Data.Where(x => mergeRequestsHandler(x));
        }
        public async Task<MergeRequest> GetMergeRequest(Project project, uint id) {
            return await GetMergeRequest(project.Id, id);
        }
        public async Task<MergeRequest> GetMergeRequest(uint projectId, uint id) {
            var mergeRequest = await client.MergeRequests.Find(projectId, id);
            return mergeRequest.Data;
        }
        public IEnumerable<CommitDiff> GetMergeRequestChanges(MergeRequest mergeRequest) {
            return mergeRequest.Changes;
        }
        public async Task<IEnumerable<Commit>> GetMergeRequestCommits(MergeRequest mergeRequest) {
            var commits = await client.MergeRequests.GetCommits(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
            return commits.Data;
        }
        public async Task<MergeRequest> ProcessMergeRequest(MergeRequest mergeRequest, string comment) {
            await client.MergeRequests.CreateComment(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, comment);
            return mergeRequest;
        }
        public async Task<MergeRequest> UpdateMergeRequestTitleAndDescription(MergeRequest mergeRequest, string title, string description) {
            await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, mergeRequest.TargetBranch, title, description, mergeRequest.Assignee.Id);
            return await GetMergeRequest(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<MergeRequest> CloseMergeRequest(MergeRequest mergeRequest) {
            await client.MergeRequests.Update(mergeRequest.ProjectId.Value, mergeRequest.Id.Value, mergeRequest.TargetBranch, state: MergeRequestStateEvent.Close);
            return await GetMergeRequest(mergeRequest.ProjectId.Value, mergeRequest.Id.Value);
        }
        public async Task<MergeRequest> CreateMergeRequest(Project origin, Project upstream, string title, string description, string user, string sourceBranch, string targetBranch) {
            var mergeRequest = await client.MergeRequests.Create(origin.Id, sourceBranch, targetBranch, title, description, targetProjectId: upstream.Id);
            return mergeRequest.Data;
        }
        public async Task<User> GetUser(uint id) {
            return (await client.Users.Find(id)).Data;
        }
        public async Task<IEnumerable<User>> GetUsers() {
            var usersClient = this.client.Users;
            return (await usersClient.GetAll()).Data;
        }
        public async Task<User> RegisterUser(string userName, string displayName, string email) {
            var user = await client.Users.Create(email, new Guid().ToString(), userName, displayName, projectsLimit: 10, confirm: true);
            return user.Data;
            //try {

            //    var userClient = this.client.Users;
            //    var userUpsert = new UserUpsert() { Username = userName, Name = displayName, Email = email, Password = new Guid().ToString() };
            //    userClient.Create(userUpsert);
            //}
            //catch (Exception ex) {
            //    Log.Error($"Can`t create user {userName} email {email}", ex);
            //    throw;
            //}
        }
        public async Task<User> RenameUser(User gitLabUser, string userName, string displayName, string email) {
            var user = await client.Users.Update((uint)gitLabUser.Id, email: email, username: userName, name: displayName);
            return user.Data;
            //try {
            //    var userClient = this.client.Users;
            //    var userUpsert = new UserUpsert() { Username = userName, Name = displayName, Email = email, Password = new Guid().ToString() };
            //    return userClient.Update(gitLabUser.Id, userUpsert);
            //}
            //catch (Exception ex) {
            //    Log.Error($"Can`t change user {userName} email {email}", ex);
            //    throw;
            //}
        }
        public async Task<Branch> GetBranch(Project project, string branchName) {
            var branch = await client.Branches.Find(project.Id, branchName);
            return branch.Data;
        }
        public async Task<IEnumerable<Branch>> GetBranches(Project project) {
            var branches = await client.Branches.GetAll(project.Id);
            return branches.Data;
        }
        public MergeRequest UpdateMergeRequestAssignee(MergeRequest mergeRequest, string user) {
            var userInfo = GetUsers().FirstOrDefault(x => x.Username == user);
            if (mergeRequest.Assignee?.Username != userInfo?.Username) {
                var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
                try {
                    return mergeRequestsClient.Update(mergeRequest.Id, new MergeRequestUpdate() {
                        AssigneeId = userInfo?.Id,
                        Title = mergeRequest.Title,
                        Description = mergeRequest.Description,
                        SourceBranch = mergeRequest.SourceBranch,
                        TargetBranch = mergeRequest.TargetBranch,
                    });
                }
                catch {
                    return mergeRequestsClient[mergeRequest.Id];
                }
            }
            return mergeRequest;
        }
        public Comment AddCommentToMergeRequest(MergeRequest mergeRequest, string comment) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var commentsClient = mergeRequestsClient.Comments(mergeRequest.Id);
            return commentsClient.Add(new MergeRequestComment() { Note = comment });
        }
        public IEnumerable<ProjectHook> GetProjectHooks(Project project) {
            var repository = this.client.GetRepository(project.Id);
            return repository.ProjectHooks.All;
        }
        public ProjectHook FindProjectHook(Project project, Func<ProjectHook, bool> projectHookHandler) {
            var projectClient = client.GetRepository(project.Id);
            var projectHooks = projectClient.ProjectHooks;
            return projectHooks.All.FirstOrDefault(projectHookHandler);
        }
        public ProjectHook CreateProjectHook(Project project, Uri url, bool mergeRequestEvents, bool pushEvents, bool buildEvents) {
            var projectClient = client.GetRepository(project.Id);
            var projectHooks = projectClient.ProjectHooks;
            return projectHooks.Create(new ProjectHookUpsert() { MergeRequestsEvents = mergeRequestEvents, PushEvents = pushEvents, BuildEvents = buildEvents, Url = url, EnableSSLVerification = false });
        }
        public ProjectHook UpdateProjectHook(Project project, ProjectHook hook, Uri uri, bool mergeRequestEvents, bool pushEvents, bool buildEvents) {
            var repository = this.client.GetRepository(project.Id);
            return repository.ProjectHooks.Update(hook.Id, new ProjectHookUpsert() { Url = uri, MergeRequestsEvents = mergeRequestEvents, PushEvents = pushEvents, BuildEvents = buildEvents, EnableSSLVerification = false });
        }
        public IEnumerable<Comment> GetComments(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var commentsClient = mergeRequestsClient.Comments(mergeRequest.Id);
            return commentsClient.All;
        }
        public bool ShouldIgnoreSharedFiles(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var commentsClient = mergeRequestsClient.Comments(mergeRequest.Id);
            var comment = commentsClient.All.LastOrDefault();
            return comment?.Note == IgnoreValidation;
        }
        public IEnumerable<MergeRequestFileData> GetFileChanges(MergeRequest mergeRequest) {
            var mergeRequestsClient = client.GetMergeRequest(mergeRequest.ProjectId);
            var changes = mergeRequestsClient.Changes(mergeRequest.Id);
            return changes.Changes.Files;
        }
        public IEnumerable<Build> GetBuilds(MergeRequest mergeRequest, Sha1 sha) {
            var projectClient = client.GetRepository(mergeRequest.SourceProjectId);
            return projectClient.Builds.GetBuildsForCommit(sha);
        }
        public void ForceBuild(MergeRequest mergeRequest, Build build = null) {
            var projectClient = client.GetRepository(mergeRequest.SourceProjectId);
            var actualBuild = build ?? projectClient.Builds.GetBuilds().FirstOrDefault();
            if (actualBuild == null || actualBuild.Status == BuildStatus.success || actualBuild.Status == BuildStatus.pending || actualBuild.Status == BuildStatus.running)
                return;
            projectClient.Builds.Retry(actualBuild);
        }
        public void AbortBuild(MergeRequest mergeRequest, Build build) {
            var projectClient = client.GetRepository(mergeRequest.SourceProjectId);
            var actualBuild = build ?? projectClient.Builds.GetBuilds().FirstOrDefault();
            if (actualBuild == null || (actualBuild.Status != BuildStatus.pending && actualBuild.Status != BuildStatus.running))
                return;
            projectClient.Builds.Cancel(actualBuild);
        }
        public byte[] DownloadArtifacts(string projectUrl, Build build) {
            Func<string, Project> findProject = IsAdmin() ? (Func<string, Project>)FindProjectFromAll : FindProject;
            var project = findProject(projectUrl);
            if (project == null)
                return null;
            var projectClient = client.GetRepository(project.Id);
            return DownloadArtifactsCore(projectClient, build);
        }
        public byte[] DownloadArtifacts(MergeRequest mergeRequest, Build build) {
            var projectClient = client.GetRepository(mergeRequest.SourceProjectId);
            return DownloadArtifactsCore(projectClient, build);
        }
        public byte[] DownloadTrace(MergeRequest mergeRequest, Build build) {
            var projectClient = client.GetRepository(mergeRequest.SourceProjectId);
            byte[] result = null;
            projectClient.Builds.GetTraceFile(build, stream => {
                if (stream == null)
                    return;
                using (MemoryStream ms = new MemoryStream()) {
                    stream.CopyTo(ms);
                    result = ms.ToArray();
                }
            });
            return result;
        }
        static byte[] DownloadArtifactsCore(IRepositoryClient projectClient, Build build) {
            byte[] result = null;
            try {
                projectClient.Builds.GetArtifactFile(build, stream => {
                    if (stream == null)
                        return;
                    using (MemoryStream ms = new MemoryStream()) {
                        stream.CopyTo(ms);
                        result = ms.ToArray();
                    }
                });
            }
            catch (Exception ex) {
                Log.Error("Can`t download artifacts.", ex);
                return null;
            }
            return result;
        }
    }
}
