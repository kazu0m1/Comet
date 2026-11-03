using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Comet.PdfWorker;

// Each worker owns exactly one PDF document and makes all PDFium calls serially.
// Communication is newline-delimited JSON on redirected standard streams.
// Rendered pages are transferred through short-lived BMP files, never executable data.
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "Comet-PdfWorker-" + Guid.NewGuid().ToString("N"));
    private static IntPtr _document;

    public static int Main()
    {
        Directory.CreateDirectory(TempRoot);
        try
        {
            Native.FPDF_InitLibrary();
            while (Console.ReadLine() is { } line)
            {
                PdfReply reply;
                try
                {
                    var request = JsonSerializer.Deserialize<PdfRequest>(line, JsonOptions)
                        ?? throw new InvalidDataException("Invalid worker request");
                    reply = Execute(request);
                }
                catch (Exception ex)
                {
                    reply = new PdfReply { Ok = false, Error = ex.Message };
                }
                Console.Out.WriteLine(JsonSerializer.Serialize(reply, JsonOptions));
                Console.Out.Flush();
                if (line.Contains("\"command\":\"close\"", StringComparison.OrdinalIgnoreCase))
                    break;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
            return 2;
        }
        finally
        {
            if (_document != IntPtr.Zero) Native.FPDF_CloseDocument(_document);
            Native.FPDF_DestroyLibrary();
            try { Directory.Delete(TempRoot, recursive: true); } catch { }
        }
    }

    private static PdfReply Execute(PdfRequest request)
    {
        switch (request.Command)
        {
            case "open":
                if (_document != IntPtr.Zero)
                {
                    Native.FPDF_CloseDocument(_document);
                    _document = IntPtr.Zero;
                }
                if (string.IsNullOrWhiteSpace(request.Path))
                    throw new ArgumentException("A PDF path is required");
                // Do not execute JavaScript, launch actions, submit forms or open attachments.
                _document = Native.FPDF_LoadDocument(request.Path, request.Password);
                if (_document == IntPtr.Zero)
                    return new PdfReply { Ok = false, Error = Native.FPDF_GetLastError() == 4 ? "Password required or incorrect" : "Cannot open PDF", PdfError = Native.FPDF_GetLastError() };
                var count = Native.FPDF_GetPageCount(_document);
                if (count < 0 || count > 100000)
                    throw new InvalidDataException("Invalid page count");
                var dimensions = new List<PageInfo>(count);
                for (var i = 0; i < count; i++)
                {
                    var page = Native.FPDF_LoadPage(_document, i);
                    if (page == IntPtr.Zero) { dimensions.Add(new PageInfo()); continue; }
                    try { dimensions.Add(new PageInfo { Width = Native.FPDF_GetPageWidthF(page), Height = Native.FPDF_GetPageHeightF(page), Label = GetPageLabel(i) }); }
                    finally { Native.FPDF_ClosePage(page); }
                }
                return new PdfReply { Ok = true, PageCount = count, Pages = dimensions, Permissions = Native.FPDF_GetDocPermissions(_document) };
            case "render":
                EnsureOpen();
                return RenderPage(request.Index, request.Width, request.Rotation);
            case "text":
                EnsureOpen();
                return ExtractText(request.Index, request.WithBoxes);
            case "outline":
                EnsureOpen();
                return new PdfReply { Ok = true, Outline = ReadOutline() };
            case "link":
                EnsureOpen();
                return HitLink(request.Index, request.X, request.Y);
            case "metadata":
                EnsureOpen();
                return new PdfReply { Ok = true, Metadata = new Dictionary<string,string>
                {
                    ["Title"] = GetMetadata("Title"), ["Author"] = GetMetadata("Author"),
                    ["Subject"] = GetMetadata("Subject"), ["Creator"] = GetMetadata("Creator"),
                    ["CreationDate"] = GetMetadata("CreationDate")
                }};
            case "close":
                return new PdfReply { Ok = true };
            default:
                throw new NotSupportedException("Unsupported PDF worker command");
        }
    }

    private static void EnsureOpen()
    {
        if (_document == IntPtr.Zero) throw new InvalidOperationException("No PDF is open");
    }

    private static PdfReply RenderPage(int index, int requestedWidth, int rotation)
    {
        var page = Native.FPDF_LoadPage(_document, index);
        if (page == IntPtr.Zero) throw new InvalidDataException("Cannot load page " + index);
        try
        {
            var pageWidth = Native.FPDF_GetPageWidthF(page);
            var pageHeight = Native.FPDF_GetPageHeightF(page);
            if (pageWidth <= 0 || pageHeight <= 0) throw new InvalidDataException("Invalid page geometry");
            var w = Math.Clamp(requestedWidth <= 0 ? 1600 : requestedWidth, 64, 4096);
            var h = checked((int)Math.Round(pageHeight * w / pageWidth));
            if (h < 1 || h > 8192 || (long)w * h > 24000000)
                throw new InvalidDataException("Page exceeds rendering safety limits");
            var bitmap = Native.FPDFBitmap_Create(w, h, 1);
            if (bitmap == IntPtr.Zero) throw new OutOfMemoryException("PDF bitmap allocation failed");
            try
            {
                Native.FPDFBitmap_FillRect(bitmap, 0, 0, w, h, 0xFFFFFFFF);
                Native.FPDF_RenderPageBitmap(bitmap, page, 0, 0, w, h, ((rotation % 4) + 4) % 4, 1);
                var stride = Native.FPDFBitmap_GetStride(bitmap);
                if (stride != w * 4) throw new InvalidDataException("Unsupported PDF bitmap stride");
                var bytes = new byte[checked(stride * h)];
                Marshal.Copy(Native.FPDFBitmap_GetBuffer(bitmap), bytes, 0, bytes.Length);
                var filename = Path.Combine(TempRoot, Guid.NewGuid().ToString("N") + ".bmp");
                using (var stream = File.Create(filename))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((ushort)0x4D42); writer.Write(checked(54 + bytes.Length));
                    writer.Write((ushort)0); writer.Write((ushort)0); writer.Write(54);
                    writer.Write(40); writer.Write(w); writer.Write(-h); // top-down bitmap
                    writer.Write((ushort)1); writer.Write((ushort)32); writer.Write(0);
                    writer.Write(bytes.Length); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                    writer.Write(bytes);
                }
                return new PdfReply { Ok = true, ImagePath = filename, Width = w, Height = h };
            }
            finally { Native.FPDFBitmap_Destroy(bitmap); }
        }
        finally { Native.FPDF_ClosePage(page); }
    }

    private static PdfReply HitLink(int index, double x, double y)
    {
        var page = Native.FPDF_LoadPage(_document, index);
        if (page == IntPtr.Zero) return new PdfReply { Ok = true };
        try
        {
            var link = Native.FPDFLink_GetLinkAtPoint(page, x, y);
            if (link == IntPtr.Zero) return new PdfReply { Ok = true };
            var dest = Native.FPDFLink_GetDest(_document, link);
            var action = Native.FPDFLink_GetAction(link);
            if (dest == IntPtr.Zero && action != IntPtr.Zero && Native.FPDFAction_GetType(action) == 1)
                dest = Native.FPDFAction_GetDest(_document, action);
            if (dest != IntPtr.Zero)
                return new PdfReply { Ok = true, LinkPageIndex = Native.FPDFDest_GetDestPageIndex(_document, dest) };
            if (action != IntPtr.Zero && Native.FPDFAction_GetType(action) == 3)
            {
                var size = Native.FPDFAction_GetURIPath(_document, action, IntPtr.Zero, 0);
                if (size > 1 && size <= 8192)
                {
                    var ptr = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        var length = Native.FPDFAction_GetURIPath(_document, action, ptr, size);
                        if (length == size)
                        {
                            var data = new byte[size];
                            Marshal.Copy(ptr, data, 0, data.Length);
                            var url = Encoding.UTF8.GetString(data).TrimEnd('\0');
                            if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                                (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                                return new PdfReply { Ok = true, LinkUrl = url };
                        }
                    }
                    finally { Marshal.FreeHGlobal(ptr); }
                }
            }
            return new PdfReply { Ok = true };
        }
        finally { Native.FPDF_ClosePage(page); }
    }

    private static PdfReply ExtractText(int index, bool includeBoxes)
    {
        var page = Native.FPDF_LoadPage(_document, index);
        if (page == IntPtr.Zero) throw new InvalidDataException("Cannot load page " + index);
        try
        {
            var textPage = Native.FPDFText_LoadPage(page);
            if (textPage == IntPtr.Zero) return new PdfReply { Ok = true, Text = "" };
            try
            {
                var count = Native.FPDFText_CountChars(textPage);
                if (count <= 0) return new PdfReply { Ok = true, Text = "" };
                if (count > 1000000) throw new InvalidDataException("Text page exceeds safety limit");
                var text = new char[count + 1];
                var copied = Native.FPDFText_GetText(textPage, 0, count, text);
                var content = new string(text, 0, Math.Clamp(copied - 1, 0, count));
                List<CharRect>? boxes = null;
                if (includeBoxes)
                {
                    boxes = new List<CharRect>(count);
                    for (var i = 0; i < count; i++)
                    {
                        if (Native.FPDFText_GetCharBox(textPage, i, out var left, out var right, out var bottom, out var top) != 0)
                            boxes.Add(new CharRect { Index = i, Left = left, Right = right, Bottom = bottom, Top = top });
                    }
                }
                return new PdfReply { Ok = true, Text = content, Boxes = boxes };
            }
            finally { Native.FPDFText_ClosePage(textPage); }
        }
        finally { Native.FPDF_ClosePage(page); }
    }

    private static List<OutlineItem> ReadOutline()
    {
        var items = new List<OutlineItem>();
        var visited = new HashSet<IntPtr>();
        void Walk(IntPtr parent, List<OutlineItem> output, int depth)
        {
            if (depth > 32 || visited.Count > 5000) return;
            var node = Native.FPDFBookmark_GetFirstChild(_document, parent);
            while (node != IntPtr.Zero && visited.Add(node))
            {
                var dest = Native.FPDFBookmark_GetDest(_document, node);
                var item = new OutlineItem { Title = GetWideString((ptr, len) => Native.FPDFBookmark_GetTitle(node, ptr, len)), PageIndex = dest == IntPtr.Zero ? -1 : Native.FPDFDest_GetDestPageIndex(_document, dest) };
                output.Add(item);
                Walk(node, item.Children, depth + 1);
                node = Native.FPDFBookmark_GetNextSibling(_document, node);
            }
        }
        Walk(IntPtr.Zero, items, 0);
        return items;
    }

    private static string GetPageLabel(int index)
        => GetWideString((ptr,len) => Native.FPDF_GetPageLabel(_document, index, ptr, len));
    private static string GetMetadata(string key)
        => GetWideString((ptr,len) => Native.FPDF_GetMetaText(_document, key, ptr, len));

    private static string GetWideString(Func<IntPtr,uint,uint> get)
    {
        var size = get(IntPtr.Zero, 0);
        if (size is < 2 or > 65536) return "";
        var mem = Marshal.AllocHGlobal((int)size);
        try
        {
            if (get(mem, size) != size) return "";
            var bytes = new byte[size];
            Marshal.Copy(mem, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        finally { Marshal.FreeHGlobal(mem); }
    }
}

internal sealed class PdfRequest
{
    public string Command { get; set; } = "";
    public string? Path { get; set; }
    public string? Password { get; set; }
    public int Index { get; set; }
    public int Width { get; set; }
    public int Rotation { get; set; }
    public bool WithBoxes { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
internal sealed class PdfReply
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public uint PdfError { get; set; }
    public int PageCount { get; set; }
    public List<PageInfo>? Pages { get; set; }
    public ulong Permissions { get; set; }
    public string? ImagePath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Text { get; set; }
    public List<CharRect>? Boxes { get; set; }
    public List<OutlineItem>? Outline { get; set; }
    public Dictionary<string,string>? Metadata { get; set; }
    public int LinkPageIndex { get; set; } = -1;
    public string? LinkUrl { get; set; }
}
internal sealed class PageInfo { public float Width { get; set; } public float Height { get; set; } public string Label { get; set; } = ""; }
internal sealed class CharRect { public int Index { get; set; } public double Left { get; set; } public double Right { get; set; } public double Bottom { get; set; } public double Top { get; set; } }
internal sealed class OutlineItem { public string Title { get; set; } = ""; public int PageIndex { get; set; } public List<OutlineItem> Children { get; set; } = new(); }

internal static class Native
{
    private const string Lib = "pdfium.dll";
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDF_InitLibrary();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDF_DestroyLibrary();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern IntPtr FPDF_LoadDocument([MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string? password);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDF_CloseDocument(IntPtr doc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern uint FPDF_GetLastError();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int FPDF_GetPageCount(IntPtr doc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDF_LoadPage(IntPtr doc, int index);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDF_ClosePage(IntPtr page);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern float FPDF_GetPageWidthF(IntPtr page);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern float FPDF_GetPageHeightF(IntPtr page);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFBitmap_Create(int w,int h,int alpha);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDFBitmap_Destroy(IntPtr bitmap);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDFBitmap_FillRect(IntPtr bitmap,int l,int t,int w,int h,uint color);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDF_RenderPageBitmap(IntPtr bitmap,IntPtr page,int x,int y,int w,int h,int rotate,int flags);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int FPDFBitmap_GetStride(IntPtr bitmap);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFText_LoadPage(IntPtr page);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern void FPDFText_ClosePage(IntPtr textPage);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int FPDFText_CountChars(IntPtr textPage);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] internal static extern int FPDFText_GetText(IntPtr textPage,int start,int count,[Out] char[] chars);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int FPDFText_GetCharBox(IntPtr textPage,int index,out double l,out double r,out double b,out double t);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern uint FPDF_GetDocPermissions(IntPtr doc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFBookmark_GetFirstChild(IntPtr doc,IntPtr bookmark);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFBookmark_GetNextSibling(IntPtr doc,IntPtr bookmark);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern uint FPDFBookmark_GetTitle(IntPtr bookmark,IntPtr buffer,uint length);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFBookmark_GetDest(IntPtr doc,IntPtr bookmark);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int FPDFDest_GetDestPageIndex(IntPtr doc,IntPtr dest);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFLink_GetLinkAtPoint(IntPtr page, double x, double y);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFLink_GetDest(IntPtr doc, IntPtr link);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFLink_GetAction(IntPtr link);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern int FPDFAction_GetType(IntPtr action);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr FPDFAction_GetDest(IntPtr doc, IntPtr action);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern uint FPDFAction_GetURIPath(IntPtr doc, IntPtr action, IntPtr buffer, uint buflen);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] internal static extern uint FPDF_GetPageLabel(IntPtr doc,int index,IntPtr buffer,uint buflen);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] internal static extern uint FPDF_GetMetaText(IntPtr doc,string tag,IntPtr buffer,uint buflen);
}
