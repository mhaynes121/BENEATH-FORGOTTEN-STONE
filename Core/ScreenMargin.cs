namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Wraps another TextWriter, inserting ScreenMargin.LeftMargin spaces at the start of every line
/// it writes -- the mechanism behind every text/menu screen's left margin (see ScreenMargin).
/// Every entry point (Write(char), Write(string), WriteLine(), WriteLine(string)) is overridden
/// explicitly rather than relying on TextWriter's own base-class chaining between them, since
/// Console.SetOut wraps whatever writer it's given in an internal synchronizing wrapper --
/// Console.Out is therefore never literally the MarginTextWriter instance itself, and depending
/// on virtual dispatch to eventually reach Write(char) proved unreliable in practice.
/// Deliberately does NOT touch the right margin; that's each screen's own wrap-width calculation
/// (TextWrapper.WrapText, GetColumnWidth, etc.), which should subtract
/// ScreenMargin.LeftMargin + ScreenMargin.RightMargin from whatever width it used to use.
/// </summary>
public sealed class MarginTextWriter : TextWriter
{
    public TextWriter Inner { get; }
    private bool atLineStart = true;

    public MarginTextWriter(TextWriter inner)
    {
        Inner = inner;
    }

    public override System.Text.Encoding Encoding => Inner.Encoding;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            Inner.Write(value);
            atLineStart = true;
            return;
        }
        if (atLineStart)
        {
            Inner.Write(new string(' ', ScreenMargin.LeftMargin));
            atLineStart = false;
        }
        Inner.Write(value);
    }

    public override void Write(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        foreach (char c in value)
        {
            Write(c);
        }
    }

    public override void WriteLine() => Write(Inner.NewLine);

    public override void WriteLine(string value)
    {
        Write(value);
        WriteLine();
    }
}

/// <summary>
/// Scopes a left-margined Console.Out to one screen's lifetime -- `using (new ScreenMargin())`
/// around a screen's render loop is the entire integration; no individual Console.WriteLine call
/// needs to change. Safe to nest (e.g. InventoryScreen opens ContainerScreen opens
/// ItemComparisonScreen): tracked via an explicit depth counter rather than inspecting
/// Console.Out's runtime type, since Console.SetOut wraps every writer in an internal
/// synchronizing wrapper that would otherwise make "is this already wrapped?" unanswerable from
/// the outside -- only the OUTERMOST instance actually wraps/restores Console.Out, so margin is
/// never doubled and an inner screen's Dispose never tears down its caller's wrapper.
///
/// One accepted, rare trade-off: if ConsoleSafety's own handle-recovery (TryReacquireOutputHandle)
/// calls Console.SetOut directly to replace a stale handle while a ScreenMargin is open, the
/// outermost Dispose still restores the pre-construction writer it originally captured, which
/// would undo that recovery. Handle-invalid errors are already a rare, already-degraded failure
/// path (see ConsoleSafety's own doc comments), so this is judged an acceptable trade-off for
/// keeping the common (nested-screen) case unambiguously correct rather than fragile.
///
/// Deliberately excludes Renderer.Render's own map-grid drawing -- that method never constructs
/// a ScreenMargin, so the live dungeon view and its glyphs are never touched by this at all.
/// </summary>
public sealed class ScreenMargin : IDisposable
{
    public const int LeftMargin = 4;
    public const int RightMargin = 4;

    [ThreadStatic]
    private static int depth;

    private readonly bool ownsWrapper;
    private readonly TextWriter previous;

    public ScreenMargin()
    {
        ownsWrapper = depth == 0;
        if (ownsWrapper)
        {
            previous = Console.Out;
            Console.SetOut(new MarginTextWriter(previous));
        }
        depth++;
    }

    public void Dispose()
    {
        depth--;
        if (ownsWrapper)
        {
            Console.SetOut(previous);
        }
    }
}
