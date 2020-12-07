namespace PrQuantifier.Core.Git
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public sealed class GitEngine
    {
        private static readonly StatusOptions RepoStatusOptions = new StatusOptions
        {
            IncludeUntracked = true,
            RecurseUntrackedDirs = true,
            IncludeIgnored = false
        };

        /// <summary>
        /// Get the git changes.
        /// </summary>
        /// <param name="path">Path to any item in the repository.</param>
        /// <returns>Dictionary of counts by operation type.</returns>
        public IEnumerable<GitFilePatch> GetGitChanges(string path)
        {
            var ret = new List<GitFilePatch>();
            var repoRoot = Repository.Discover(path);
            using var repo = new Repository(repoRoot);

            var trackedFilesPatch = repo.Diff.Compare<Patch>();
            ret.AddRange(GetGitFilePatch(trackedFilesPatch));

            var status = repo.RetrieveStatus(RepoStatusOptions);
            if (status.Untracked.Any())
            {
                var untrackedFilesPatch = repo.Diff.Compare<Patch>(
                    status.Untracked.Select(u => u.FilePath), true, new ExplicitPathsOptions());
                ret.AddRange(GetGitFilePatch(untrackedFilesPatch));
            }

            return ret;
        }

        private IEnumerable<GitFilePatch> GetGitFilePatch(Patch filesPatch)
        {
            var ret = new List<GitFilePatch>();
            using IEnumerator<PatchEntryChanges> patches = filesPatch.GetEnumerator();
            while (patches.MoveNext()
                   && patches.Current != null)
            {
                ret.Add(new GitFilePatch
                {
                    DiffContent = patches.Current.Patch,
                    AbsoluteLinesAdded = patches.Current.LinesAdded,
                    AbsoluteLinesDeleted = patches.Current.LinesDeleted,
                    FilePath = patches.Current.Path,
                    ChangeType = patches.Current.Status
                });
            }

            return ret;
        }
    }
}
