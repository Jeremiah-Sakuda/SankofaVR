using System;
using System.IO;
using System.Text;
using Sankofa.Core.Personal;

namespace Sankofa.Storage
{
    /// <summary>One explicit local Keep. The realised story is saved; raw intake and provider credentials are not.</summary>
    public sealed class PersonalStoryStore
    {
        readonly string folder;
        string Shelf => Path.Combine(folder, "kept.story");
        public PersonalStoryStore(string directory) { folder = directory ?? throw new ArgumentNullException(nameof(directory)); }
        public bool HasKept => File.Exists(Shelf) && File.ReadAllText(Shelf) != "forgotten";
        public PersonalStoryDocument Load()
        {
            if (!File.Exists(Shelf)) return null;
            var text = File.ReadAllText(Shelf, Encoding.UTF8);
            return text == "forgotten" ? null : PersonalStoryJson.ReadValidated(text);
        }
        public void Keep(PersonalStoryDocument story)
        {
            var report = PersonalStoryValidator.Validate(story);
            if (!report.IsValid) throw new InvalidDataException("Only a checked story can be kept.");
            Write(PersonalStoryJson.Write(story));
        }
        public void Forget() => Write("forgotten");
        void Write(string text)
        {
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
    }
}
