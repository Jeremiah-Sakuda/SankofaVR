using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;

namespace Sankofa.Storage
{
    /// <summary>
    /// One active kept shelf in the application's private folder. Replacement atomically writes the complete visit;
    /// a durable empty shelf prevents obsolete legacy JSON files from resurfacing after forgetting. Per-visit Save
    /// remains for legacy callers. Unreadable records are reported rather than silently substituted or deleted.
    /// </summary>
    public sealed class FileVisitStore : IVisitStore
    {
        const string Extension = ".json";
        const string Temporary = ".tmp";
        const string Shelf = "current.visit";
        const string EmptyShelf = "forgotten";
        static readonly Encoding Utf8 = new UTF8Encoding(false);

        public FileVisitStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A folder is required", nameof(directory));
            Directory = directory;
        }

        public string Directory { get; }

        public VisitStoreContents Load()
        {
            var visits = new List<SavedVisit>();
            var problems = new List<string>();
            if (!System.IO.Directory.Exists(Directory)) return new VisitStoreContents(visits, problems);
            var shelf = Path.Combine(Directory, Shelf);
            if (File.Exists(shelf))
            {
                try
                {
                    var text = File.ReadAllText(shelf, Utf8);
                    if (text != EmptyShelf) visits.Add(SavedVisitCodec.Read(text));
                }
                catch (Exception e) when (e is StoryFormatException || e is IOException || e is UnauthorizedAccessException)
                {
                    problems.Add("The kept visit could not be read: " + e.Message);
                }
                // A corrupt or empty active shelf never falls back to obsolete legacy visits.
                return new VisitStoreContents(visits, problems);
            }
            string[] files;
            try
            {
                files = System.IO.Directory.GetFiles(Directory, "*" + Extension);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                problems.Add("Kept visits could not be listed: " + e.Message);
                return new VisitStoreContents(visits, problems);
            }
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                try
                {
                    var visit = SavedVisitCodec.Read(File.ReadAllText(file, Utf8));
                    if (visit.Id + Extension != name) problems.Add($"{name}: its id '{visit.Id}' does not match the file name");
                    else visits.Add(visit);
                }
                catch (StoryFormatException e)
                {
                    problems.Add($"{name}: {e.Message}");
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    problems.Add($"{name}: {e.Message}");
                }
            }
            return new VisitStoreContents(visits, problems);
        }

        public void Save(SavedVisit visit)
        {
            if (visit == null) throw new ArgumentNullException(nameof(visit));
            var target = PathFor(visit.Id);
            var temporary = target + Temporary;
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                var bytes = Utf8.GetBytes(SavedVisitCodec.Write(visit));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                TryDelete(temporary);
                throw new VisitStoreException("The visit could not be kept: " + e.Message, e);
            }
        }

        public void Replace(SavedVisit visit)
        {
            if (visit == null) throw new ArgumentNullException(nameof(visit));
            WriteShelf(SavedVisitCodec.Write(visit));
        }

        void WriteShelf(string text)
        {
            var target = Path.Combine(Directory, Shelf);
            var temporary = target + Temporary;
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                var bytes = Utf8.GetBytes(text);
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                TryDelete(temporary);
                throw new VisitStoreException("The kept shelf could not be updated: " + e.Message, e);
            }
        }

        public void Delete(string id)
        {
            var target = PathFor(id);
            try
            {
                var shelf = Path.Combine(Directory, Shelf);
                var forgettingActive = !File.Exists(shelf) && Load().Visits.FirstOrDefault()?.Id == id;
                if (File.Exists(shelf))
                {
                    var text = File.ReadAllText(shelf, Utf8);
                    forgettingActive = text == EmptyShelf || SavedVisitCodec.Read(text).Id == id;
                }
                if (forgettingActive)
                {
                    // Commit the empty shelf before cleanup. A crash or locked obsolete file cannot revive it.
                    WriteShelf(EmptyShelf);
                    foreach (var legacy in System.IO.Directory.GetFiles(Directory, "*" + Extension))
                    {
                        try { SavedVisitCodec.Read(File.ReadAllText(legacy, Utf8)); }
                        catch (StoryFormatException) { continue; }
                        File.Delete(legacy);
                    }
                }
                if (File.Exists(target)) File.Delete(target);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is StoryFormatException)
            {
                throw new VisitStoreException("The visit could not be forgotten: " + e.Message, e);
            }
        }

        string PathFor(string id)
        {
            if (!SavedVisitCodec.IsValidId(id)) throw new VisitStoreException($"'{id}' is not a valid visit id");
            return Path.Combine(Directory, id + Extension);
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // The temporary file is ignored by Load; a later save overwrites it.
            }
        }
    }
}
