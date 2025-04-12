using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices;

namespace AWS3.Models;

public class S3ResponseDTO
{
    public int StatusCode { get; set; } = 200;
    public string Message { get; set; } = "";
}