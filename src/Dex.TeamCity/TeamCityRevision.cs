#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

#endregion

namespace Dex.TeamCity
{
    public static class TeamCityRevision
    {
        public static TeamCityRevisionDto GetByDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                var file = GetRevisionFile(Directory.GetFiles(directory, "*.*"));
                if (file != null)
                {
                    return GetDto(file, File.ReadAllText(file));
                }
            }

            return TeamCityRevisionDto.Create();
        }

        public static TeamCityRevisionDto GetByZipFile(string zipFileName)
        {
            if (File.Exists(zipFileName))
            {
                using var archive = ZipFile.OpenRead(zipFileName);
                var file = GetRevisionFile(archive.Entries.Select(e => e.FullName));
                if (file != null)
                {
                    var entry = archive.Entries.First(e => e.FullName == file);
                    using var entryStream = entry.Open();
                    using TextReader reader = new StreamReader(entryStream);
                    return GetDto(file, reader.ReadToEnd());
                }
            }

            return TeamCityRevisionDto.Create();
        }

        private static TeamCityRevisionDto GetDto(string filePath, string content)
        {
            var result = new TeamCityRevisionDto();
            var fileName = Path.GetFileName(filePath);

            if (fileName != null)
            {
                var split = fileName.Split('.');
                result.Build = Convert.ToInt32(split[0], CultureInfo.InvariantCulture);
                result.Revision = content;
                result.TeamCityRevision = fileName;
            }

            return result;
        }

        private static string GetRevisionFile(IEnumerable<string> files)
        {
            files = files.ToArray();
            return files.Select(f =>
                {
                    var fileName = Path.GetFileName(f);

                    if (fileName != null)
                    {
                        var s1 = fileName.Split('.')[0];
                        if (int.TryParse(s1, out var temp))
                        {
                            return new
                            {
                                number = temp,
                                path = f
                            };
                        }
                    }

                    return null;
                })
                .Where(i => i != null)
                .OrderByDescending(i => i.number)
                .Select(i => i.path)
                .FirstOrDefault();
        }
    }
}