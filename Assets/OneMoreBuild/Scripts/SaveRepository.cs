using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OneMoreBuild
{
    // The serializer is injected so disk recovery is testable without Unity.
    public sealed class SaveRepository
    {
        readonly string path;
        readonly Func<GameState, string> encode;
        readonly Func<string, GameState> decode;
        const string Header = "ONE_MORE_BUILD_1";
        public SaveRepository(string path, Func<GameState, string> encode, Func<string, GameState> decode)
        { this.path = path; this.encode = encode; this.decode = decode; }
        static string Hash(string body)
        {
            using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(body)));
        }
        public bool Save(GameState state, out string error)
        {
            error = null;
            try
            {
                Simulation.Migrate(state);
            if (!Simulation.IsValid(state)) throw new InvalidDataException("Состояние игры не прошло проверку.");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string body = encode(state);
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, Header + "\n" + Hash(body) + "\n" + body, Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception e) when (e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is System.Security.SecurityException)
            { error = e.Message; return false; }
        }
        GameState Read(string file)
        {
            string text = File.ReadAllText(file, Encoding.UTF8);
            string[] parts = text.Split(new[] { '\n' }, 3);
            if (parts.Length != 3 || parts[0] != Header || parts[1] != Hash(parts[2])) throw new InvalidDataException("Файл повреждён или имеет неизвестный формат.");
            GameState state = decode(parts[2]);
            Simulation.Migrate(state);
            if (!Simulation.IsValid(state)) throw new InvalidDataException("Некорректные данные сохранения.");
            return state;
        }
        public GameState Load(out string message)
        {
            message = null;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return new GameState();
            try { return Read(path); }
            catch (Exception e) when (e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is System.Security.SecurityException)
            {
                try
                {
                    GameState recovered = Read(path + ".bak");
                    Quarantine();
                    message = "Основное сохранение повреждено. Восстановлена резервная копия.";
                    return recovered;
                }
                catch (Exception backupError) when (backupError is InvalidDataException || backupError is IOException || backupError is UnauthorizedAccessException || backupError is ArgumentException || backupError is System.Security.SecurityException)
                {
                    Quarantine();
                    message = "Сохранение прочитать не удалось. Начата новая игра; исходные файлы сохранены с расширением .corrupt.";
                    return new GameState();
                }
            }
        }
        void Quarantine()
        {
            // Never overwrite unreadable player data on the next autosave.
            try
            {
                string suffix = "." + DateTime.UtcNow.Ticks + ".corrupt";
                if (File.Exists(path)) File.Move(path, path + suffix);
                // Keep a valid backup available; preserve a corrupt one separately.
                if (File.Exists(path + ".bak"))
                {
                    try { Read(path + ".bak"); }
                    catch (Exception e) when (e is InvalidDataException || e is IOException || e is ArgumentException) { File.Move(path + ".bak", path + ".bak" + suffix); }
                }
            }
            catch (Exception e) when (e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is System.Security.SecurityException) { /* Disk remains read-only: Save reports its own error. */ }
        }
    }
}
