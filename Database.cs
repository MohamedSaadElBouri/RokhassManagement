using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace LicenseManagement
{
    internal static class Database
    {
        // Base dans ...\bin\(x64|x86)\(Debug|Release)\data\licenses.db
        private static readonly string DbFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "licenses.db");

        private static string ConnString
        {
            get { return $"Data Source={DbFilePath};Version=3;Foreign Keys=True;"; }
        }

        public static void Initialize()
        {
            // Créer le dossier + fichier si besoin
            var dir = Path.GetDirectoryName(DbFilePath);
            //if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            //if (!File.Exists(DbFilePath)) SQLiteConnection.CreateFile(DbFilePath);

            using (var cn = new SQLiteConnection(ConnString))
            {
                cn.Open();

                // La table existe ?
                bool tableExists;
                using (var check = new SQLiteCommand(
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name='Licenses' LIMIT 1;", cn))// Recherche à la table
                {
                    tableExists = check.ExecuteScalar() != null;
                }

                if (!tableExists)
                {
                    // Schéma attendu par l'appli
                    string createSql = @"
CREATE TABLE Licenses (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Type            TEXT NOT NULL,
    Reference       TEXT NOT NULL,
    Date            DATETIME NOT NULL,
    Status          TEXT NOT NULL DEFAULT 'نشط',
    SubType         TEXT DEFAULT '-',
    Sifa            TEXT DEFAULT 'شخص ذاتي',
    Nom             TEXT DEFAULT '',
    Activite        TEXT DEFAULT '',
    DateAnnulation  DATETIME NULL
);
CREATE INDEX IF NOT EXISTS IX_Licenses_Reference ON Licenses(Reference);";
                    using (var cmd = new SQLiteCommand(createSql, cn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    return; // table fraîche créée
                }

                // Table existante : ajouter les colonnes manquantes
                var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var pragma = new SQLiteCommand("PRAGMA table_info(Licenses);", cn))
                using (var r = pragma.ExecuteReader())
                {
                    while (r.Read()) have.Add(r["name"].ToString());
                }

                Action<string, string> AddColIfMissing = (name, defSql) =>
                {
                    if (!have.Contains(name))
                    {
                        using (var alter = new SQLiteCommand(
                            $"ALTER TABLE Licenses ADD COLUMN {name} {defSql};", cn))
                        {
                            alter.ExecuteNonQuery();
                        }
                    }
                };

                // Colonnes de base
                AddColIfMissing("Type", "TEXT");
                AddColIfMissing("Reference", "TEXT");
                AddColIfMissing("Date", "DATETIME");
                AddColIfMissing("Status", "TEXT DEFAULT 'نشط'");
                // Colonnes additionnelles
                AddColIfMissing("SubType", "TEXT DEFAULT '-'");
                AddColIfMissing("Sifa", "TEXT DEFAULT 'شخص ذاتي'");
                AddColIfMissing("Nom", "TEXT DEFAULT ''");
                AddColIfMissing("Activite", "TEXT DEFAULT ''");
                AddColIfMissing("DateAnnulation", "DATETIME NULL");
            }
        }

        public static SQLiteConnection GetConnection()
        {
            var cn = new SQLiteConnection(ConnString);
            cn.Open();
            return cn;
        }
    }
}
