using System;
using System.IO;
using System.Xml;

namespace SysmacXmlViewer.Services;

public static class ErrorMessages
{
    public static string Describe(Exception error)
    {
        // Only application-owned messages are safe to display independently of OS language.
        if (error is XmlParseException && error.InnerException is { } cause)
            return "Failed to parse the XML file: " + Describe(cause);
        switch (error.Message)
        {
            case "The Sysmac backup Content element is missing.":
            case "The Header element is missing.":
            case "The RetainVariable element is missing.":
            case "The Project element is missing.":
            case "Each variable requires Name, DataType and Data.":
            case "The backup has no valid device tracking ID.":
            case "The project must contain exactly one device matching the backup tracking ID.":
            case "A project variable table is missing.":
            case "Unsupported project variable table format.":
                return error.Message;
        }
        return error switch
        {
            FileNotFoundException or DirectoryNotFoundException => "The file or folder was not found. Check the path and try again.",
            UnauthorizedAccessException or System.Security.SecurityException => "Access was denied. Check file and folder permissions.",
            PathTooLongException => "The file path is too long. Choose a shorter path.",
            InvalidDataException => "The file is damaged or uses an unsupported format.",
            XmlException xml => $"The XML is invalid (line {xml.LineNumber}, position {xml.LinePosition}).",
            IOException => "The file could not be read or written. Check whether it is in use and whether the drive has enough free space.",
            ArgumentException or NotSupportedException => "The path, value or file format is not supported.",
            _ => $"An unexpected error occurred ({error.GetType().Name}, code 0x{error.HResult:X8})."
        };
    }
}
