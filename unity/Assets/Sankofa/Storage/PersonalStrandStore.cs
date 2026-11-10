using System;
using System.IO;
using System.Text;
using Sankofa.Core.Personal;

namespace Sankofa.Storage
{
    /// <summary>
    /// The visitor's strand of kept sides, in one file beside the kept telling. It holds identifiers only. A missing,
    /// unreadable or damaged file is an empty strand, so a bad file never blocks a visit; a failed save or forget is
    /// thrown to the caller, so the application never shows a bead that was not kept.
    /// </summary>
    public sealed class PersonalStrandStore
    {
        readonly string folder;
        string Shelf => Path.Combine(folder, "kept.strand");
        public PersonalStrandStore(string directory) { folder = directory ?? throw new ArgumentNullException(nameof(directory)); }

        public KeptStrand Load()
        {
            try
            {
                if (!File.Exists(Shelf)) return new KeptStrand();
                return KeptStrandJson.Parse(File.ReadAllText(Shelf, Encoding.UTF8));
            }
            catch (Exception e) when (e is FormatException || e is IOException || e is UnauthorizedAccessException)
            {
                return new KeptStrand();
            }
        }

        /// <summary>Replaces the stored strand in one step. Throws when it could not be written; the earlier file is then unchanged.</summary>
        public void Save(KeptStrand strand)
        {
            var text = KeptStrandJson.Write(strand);
            Directory.CreateDirectory(folder);
            var temporary = Shelf + ".tmp";
            try
            {
                var bytes = new UTF8Encoding(false).GetBytes(text);
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(Shelf)) File.Replace(temporary, Shelf, null);
                else File.Move(temporary, Shelf);
            }
            catch
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { }
                throw;
            }
        }

        /// <summary>Removes the strand. Forgetting an absent strand succeeds; a file that cannot be removed is thrown to the caller.</summary>
        public void Forget()
        {
            if (File.Exists(Shelf)) File.Delete(Shelf);
            var temporary = Shelf + ".tmp";
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
