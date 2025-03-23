using System.Text.Json;
using MauiApp1.Models;

namespace MauiApp1.Services;

public class ProfileService : ContentPage
{
	private const string DataFileName = "data.json";
	       private readonly string WritableDataFilePath = Path.Combine(FileSystem.AppDataDirectory, DataFileName);
		   
		           public async Task<DataModel> GetDataAsync()
        {
            try
            {
                // ตรวจสอบว่ามีไฟล์ในโฟลเดอร์ที่เขียนได้หรือไม่
                if (!File.Exists(WritableDataFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Copying data.json to writable directory.");
                    using var stream = await FileSystem.OpenAppPackageFileAsync(DataFileName);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    await File.WriteAllTextAsync(WritableDataFilePath, content);
                }

                // อ่านไฟล์จากโฟลเดอร์ที่เขียนได้
                var json = await File.ReadAllTextAsync(WritableDataFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<DataModel>(json, options);

                if (data == null)
                {
                    System.Diagnostics.Debug.WriteLine("Deserialized data is null");
                    return new DataModel();
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {data.Students.Count} students and {data.Courses.Count} courses.");
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading JSON: {ex.Message}");
                return new DataModel();
            }
        }

}