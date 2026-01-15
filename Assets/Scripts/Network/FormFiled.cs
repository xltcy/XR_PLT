using System.IO;

public enum FormFieldType
{
    Text,     // 文本字段
    Binary,   // 二进制数据
    File      // 文件（带文件名）
}

public class FormField
{
    public string FieldName { get; set; }
    public FormFieldType Type { get; set; }
    public string StringValue { get; set; }
    public byte[] BinaryValue { get; set; }
    public string FileName { get; set; }
    public string MimeType { get; set; }

    // 便捷构造函数
    public static FormField CreateText(string name, string value)
    {
        return new FormField
        {
            FieldName = name,
            Type = FormFieldType.Text,
            StringValue = value,
        };
    }

    public static FormField CreateBinary(string name, byte[] data, string fileName = null)
    {
        return new FormField
        {
            FieldName = name,
            Type = FormFieldType.Binary,
            BinaryValue = data,
            FileName = fileName ?? "data.bin",
            MimeType = "application/octet-stream"
        };
    }

    public static FormField CreateFile(string name, byte[] fileData, string fileName, string mimeType = null)
    {
        return new FormField
        {
            FieldName = name,
            Type = FormFieldType.File,
            BinaryValue = fileData,
            FileName = fileName,
            MimeType = mimeType ?? GetMimeType(fileName)
        };
    }

    public static string GetMimeType(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".zip" => "application/zip",
            ".mp3" => "audio/mpeg",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}