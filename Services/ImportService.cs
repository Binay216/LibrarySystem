using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services.Csv;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    public class ImportService(ApplicationDbContext context) : IImportService
    {
        private static readonly string[] ExpectedHeaders =
        [
            "Type", "Name", "Description", "LibraryCode", "HomeBranchCode",
            "Author", "Genre", "Artist", "Year", "ToyType", "RecommendedAge"
        ];

        public async Task<ImportPreviewResult> PreviewAsync(string csvText)
        {
            var (rows, fileError) = await ParseAndValidateAsync(csvText);
            return new ImportPreviewResult { Rows = rows, FileError = fileError };
        }

        public async Task<ImportCommitResult> CommitAsync(string csvText, IReadOnlySet<int> selectedRowNumbers)
        {
            var (rows, fileError) = await ParseAndValidateAsync(csvText);
            if (fileError is not null)
            {
                return new ImportCommitResult { FileError = fileError };
            }

            var rejected = rows.Where(r => !r.IsValid).ToList();
            var skipped = rows.Where(r => r.IsValid && !selectedRowNumbers.Contains(r.RowNumber)).ToList();
            var toImport = rows.Where(r => r.IsValid && selectedRowNumbers.Contains(r.RowNumber)).ToList();

            if (toImport.Count == 0)
            {
                return new ImportCommitResult { Created = 0, Skipped = skipped.Count, Rejected = rejected.Count };
            }

            // A single SaveChangesAsync() call is already atomic in EF Core, but the
            // transaction is made explicit here so "a mid-file failure leaves no
            // partial data" is visible in the code, not just an implicit framework
            // behaviour a reader has to already know.
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in toImport)
                {
                    context.Items.Add(BuildItem(row));
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return new ImportCommitResult
                {
                    Created = 0,
                    Skipped = skipped.Count,
                    Rejected = rejected.Count + toImport.Count,
                    FileError = "Import failed and was rolled back - no items were created. " +
                        "This can happen if a library code was taken by someone else at the exact same moment; re-upload the file to retry."
                };
            }

            return new ImportCommitResult { Created = toImport.Count, Skipped = skipped.Count, Rejected = rejected.Count };
        }

        public async Task<MetadataImportResult> ImportMetadataRecordAsync(MetadataRecord record, string libraryCode, int homeBranchId)
        {
            var code = libraryCode.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return new MetadataImportResult { Success = false, ErrorMessage = "LibraryCode is required." };
            }

            var codeTaken = await context.Items.AsNoTracking().AnyAsync(i => i.LibraryCode == code);
            if (codeTaken)
            {
                return new MetadataImportResult { Success = false, ErrorMessage = $"Library code '{code}' is already in use." };
            }

            var branchExists = await context.Branches.AsNoTracking().AnyAsync(b => b.Id == homeBranchId);
            if (!branchExists)
            {
                return new MetadataImportResult { Success = false, ErrorMessage = "Unknown branch." };
            }

            Item item = record.Type.Trim().ToUpperInvariant() switch
            {
                "BOOK" => new Book { Author = record.Author.Trim(), Genre = record.Genre.Trim() },
                "MUSIC" => new Music { Artist = record.Artist.Trim(), Year = record.Year ?? 0 },
                "TOY" => new Toy { ToyType = record.ToyType.Trim(), RecommendedAge = record.RecommendedAge ?? 0 },
                _ => throw new InvalidOperationException($"Unknown item type '{record.Type}'.")
            };

            item.Name = record.Title.Trim();
            item.Description = record.Description.Trim();
            item.LibraryCode = code;
            item.Status = ItemStatus.Available;
            item.HomeBranchId = homeBranchId;
            item.CurrentBranchId = homeBranchId;

            context.Items.Add(item);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return new MetadataImportResult { Success = false, ErrorMessage = $"Library code '{code}' is already in use." };
            }

            return new MetadataImportResult { Success = true };
        }

        private async Task<(List<ImportRowPreview> Rows, string? FileError)> ParseAndValidateAsync(string csvText)
        {
            var records = CsvParser.Parse(csvText);
            if (records.Count == 0)
            {
                return ([], "The file is empty.");
            }

            var header = records[0];
            if (!HeaderMatches(header))
            {
                return ([], $"The file is missing expected columns. Expected: {string.Join(", ", ExpectedHeaders)}.");
            }

            var branches = await context.Branches.AsNoTracking()
                .ToDictionaryAsync(b => b.Code.Trim().ToUpperInvariant());
            var existingLibraryCodes = (await context.Items.AsNoTracking().Select(i => i.LibraryCode).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seenInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var rows = new List<ImportRowPreview>();

            for (var r = 1; r < records.Count; r++)
            {
                var rowNumber = r + 1;
                var map = MapRow(header, records[r]);
                rows.Add(ValidateRow(rowNumber, map, branches, existingLibraryCodes, seenInFile));
            }

            return (rows, null);
        }

        private static ImportRowPreview ValidateRow(
            int rowNumber,
            Dictionary<string, string> map,
            Dictionary<string, Branch> branches,
            HashSet<string> existingLibraryCodes,
            Dictionary<string, int> seenInFile)
        {
            var type = map.GetValueOrDefault("Type", "");
            var name = map.GetValueOrDefault("Name", "");
            var description = map.GetValueOrDefault("Description", "");
            var libraryCode = map.GetValueOrDefault("LibraryCode", "");
            var homeBranchCode = map.GetValueOrDefault("HomeBranchCode", "");
            var author = map.GetValueOrDefault("Author", "");
            var genre = map.GetValueOrDefault("Genre", "");
            var artist = map.GetValueOrDefault("Artist", "");
            var year = map.GetValueOrDefault("Year", "");
            var toyType = map.GetValueOrDefault("ToyType", "");
            var recommendedAge = map.GetValueOrDefault("RecommendedAge", "");

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                errors.Add("Description is required.");
            }

            if (string.IsNullOrWhiteSpace(libraryCode))
            {
                errors.Add("LibraryCode is required.");
            }

            if (string.IsNullOrWhiteSpace(homeBranchCode))
            {
                errors.Add("HomeBranchCode is required.");
            }

            var normalizedType = type.Trim().ToUpperInvariant();
            var isKnownType = normalizedType is "BOOK" or "MUSIC" or "TOY";
            if (string.IsNullOrWhiteSpace(type))
            {
                errors.Add("Type is required.");
            }
            else if (!isKnownType)
            {
                errors.Add($"Unknown item type '{type}'. Expected Book, Music or Toy.");
            }

            int? homeBranchId = null;
            if (!string.IsNullOrWhiteSpace(homeBranchCode))
            {
                if (branches.TryGetValue(homeBranchCode.Trim().ToUpperInvariant(), out var branch))
                {
                    homeBranchId = branch.Id;
                }
                else
                {
                    errors.Add($"Unknown branch code '{homeBranchCode}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(libraryCode))
            {
                if (existingLibraryCodes.Contains(libraryCode))
                {
                    errors.Add($"Library code '{libraryCode}' is already in use.");
                }
                else if (seenInFile.TryGetValue(libraryCode, out var firstRow))
                {
                    errors.Add($"Library code '{libraryCode}' is duplicated in this file (first seen on row {firstRow}).");
                }
                else
                {
                    seenInFile[libraryCode] = rowNumber;
                }
            }

            if (isKnownType)
            {
                switch (normalizedType)
                {
                    case "BOOK":
                        if (string.IsNullOrWhiteSpace(author))
                        {
                            errors.Add("Author is required for a Book.");
                        }

                        if (string.IsNullOrWhiteSpace(genre))
                        {
                            errors.Add("Genre is required for a Book.");
                        }

                        break;
                    case "MUSIC":
                        if (string.IsNullOrWhiteSpace(artist))
                        {
                            errors.Add("Artist is required for Music.");
                        }

                        if (!int.TryParse(year, out var yearValue) || yearValue < 1900 || yearValue > 2100)
                        {
                            errors.Add("Year must be a whole number between 1900 and 2100 for Music.");
                        }

                        break;
                    case "TOY":
                        if (string.IsNullOrWhiteSpace(toyType))
                        {
                            errors.Add("ToyType is required for a Toy.");
                        }

                        if (!int.TryParse(recommendedAge, out var ageValue) || ageValue < 0 || ageValue > 18)
                        {
                            errors.Add("RecommendedAge must be a whole number between 0 and 18 for a Toy.");
                        }

                        break;
                }
            }

            return new ImportRowPreview
            {
                RowNumber = rowNumber,
                IsValid = errors.Count == 0,
                Errors = errors,
                Type = type,
                Name = name,
                Description = description,
                LibraryCode = libraryCode,
                HomeBranchCode = homeBranchCode,
                Author = author,
                Genre = genre,
                Artist = artist,
                Year = year,
                ToyType = toyType,
                RecommendedAge = recommendedAge,
                HomeBranchId = homeBranchId
            };
        }

        private static Item BuildItem(ImportRowPreview row)
        {
            Item item = row.Type.Trim().ToUpperInvariant() switch
            {
                "BOOK" => new Book { Author = row.Author.Trim(), Genre = row.Genre.Trim() },
                "MUSIC" => new Music { Artist = row.Artist.Trim(), Year = int.Parse(row.Year.Trim()) },
                "TOY" => new Toy { ToyType = row.ToyType.Trim(), RecommendedAge = int.Parse(row.RecommendedAge.Trim()) },
                _ => throw new InvalidOperationException($"Unknown item type '{row.Type}'.")
            };

            item.Name = row.Name.Trim();
            item.Description = row.Description.Trim();
            item.LibraryCode = row.LibraryCode.Trim();
            // Newly imported stock starts Available, at its own home branch - the
            // same defaults the single-item Create form uses.
            item.Status = ItemStatus.Available;
            item.HomeBranchId = row.HomeBranchId!.Value;
            item.CurrentBranchId = row.HomeBranchId!.Value;
            return item;
        }

        private static Dictionary<string, string> MapRow(string[] header, string[] fields)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Length; i++)
            {
                map[header[i].Trim()] = i < fields.Length ? fields[i] : "";
            }

            return map;
        }

        private static bool HeaderMatches(string[] header)
        {
            var normalized = header.Select(h => h.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return ExpectedHeaders.All(normalized.Contains);
        }
    }
}
