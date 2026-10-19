using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("模拟器串口配置")]
[assembly: System.Reflection.AssemblyDescription("把本机串口映射到雷电模拟器（UART HostDevice）")]
[assembly: System.Reflection.AssemblyProduct("模拟器串口配置")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace LdCom
{
    // ==================================================================
    // 主题
    // ==================================================================
    static class T
    {
        public static Color Bg = Color.FromArgb(0xF3, 0xF5, 0xF9);
        public static Color Card = Color.White;
        public static Color Border = Color.FromArgb(0xE4, 0xE9, 0xF2);
        public static Color BorderStrong = Color.FromArgb(0xD3, 0xDC, 0xEB);
        public static Color Text = Color.FromArgb(0x1B, 0x24, 0x33);
        public static Color TextSoft = Color.FromArgb(0x44, 0x52, 0x67);
        public static Color Sub = Color.FromArgb(0x77, 0x84, 0x9B);
        public static Color Muted = Color.FromArgb(0xA5, 0xB0, 0xC4);
        public static Color Accent = Color.FromArgb(0x2F, 0x6B, 0xFF);
        public static Color Accent2 = Color.FromArgb(0x5C, 0x97, 0xFF);
        public static Color AccentSoft = Color.FromArgb(0xEC, 0xF3, 0xFF);
        public static Color Green = Color.FromArgb(0x16, 0xA3, 0x4A);
        public static Color GreenSoft = Color.FromArgb(0xE9, 0xF9, 0xEF);
        public static Color Red = Color.FromArgb(0xD6, 0x45, 0x45);
        public static Color Amber = Color.FromArgb(0xB4, 0x6B, 0x00);
        public static string FontName = "Microsoft YaHei UI";
    }

    static class Ui
    {
        public static float S = 1f;
        public static int Z(float v) { return (int)Math.Round(v * S); }
    }

    class Fonts
    {
        public Font H1, H2, Body, BodyBold, Small, SmallBold, RowTitle, RowSub, Btn, BtnBold, Tiny, TinyBold;

        public Fonts(float s)
        {
            string n = T.FontName;
            H1 = new Font(n, 12.5f * s, FontStyle.Bold, GraphicsUnit.Point);
            H2 = new Font(n, 10.5f * s, FontStyle.Bold, GraphicsUnit.Point);
            Body = new Font(n, 9f * s, FontStyle.Regular, GraphicsUnit.Point);
            BodyBold = new Font(n, 9f * s, FontStyle.Bold, GraphicsUnit.Point);
            Small = new Font(n, 8.25f * s, FontStyle.Regular, GraphicsUnit.Point);
            SmallBold = new Font(n, 8.25f * s, FontStyle.Bold, GraphicsUnit.Point);
            RowTitle = new Font(n, 10f * s, FontStyle.Bold, GraphicsUnit.Point);
            RowSub = new Font(n, 8.25f * s, FontStyle.Regular, GraphicsUnit.Point);
            Btn = new Font(n, 9.5f * s, FontStyle.Regular, GraphicsUnit.Point);
            BtnBold = new Font(n, 9.5f * s, FontStyle.Bold, GraphicsUnit.Point);
            Tiny = new Font(n, 7.5f * s, FontStyle.Regular, GraphicsUnit.Point);
            TinyBold = new Font(n, 7.5f * s, FontStyle.Bold, GraphicsUnit.Point);
        }
    }

    static class F { public static Fonts I; }

    // ==================================================================
    // 绘图工具
    // ==================================================================
    static class Gfx
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return p;
            if (radius <= 0) { p.AddRectangle(r); return p; }
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Fill(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
        }

        public static void FillGrad(Graphics g, Rectangle r, int radius, Color c1, Color c2)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (GraphicsPath p = Round(r, radius))
            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(r.X, r.Y, Math.Max(1, r.Width), Math.Max(1, r.Height)), c1, c2, LinearGradientMode.Vertical))
                g.FillPath(b, p);
        }

        public static void Stroke(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(c)) g.DrawPath(pen, p);
        }

        public static void Ring(Graphics g, Rectangle r, int radius, Color c, float w)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(c, w)) g.DrawPath(pen, p);
        }

        public static int Measure(Graphics g, string s, Font f)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return TextRenderer.MeasureText(g, s, f, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }

        public static void TextL(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            TextRenderer.DrawText(g, s, f, r, c, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }

        public static void TextLNoEllipsis(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            TextRenderer.DrawText(g, s, f, r, c, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        public static void TextLPath(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            TextRenderer.DrawText(g, s, f, r, c, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.PathEllipsis);
        }

        public static void TextR(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            int w = Measure(g, s, f);
            Rectangle rr = new Rectangle(r.Right - w - 2, r.Y, w + 2, r.Height);
            TextRenderer.DrawText(g, s, f, rr, c, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        public static void TextC(Graphics g, string s, Font f, Rectangle r, Color c)
        {
            TextRenderer.DrawText(g, s, f, r, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        // 绘制一个圆角小胶囊标签
        public static int Badge(Graphics g, string text, Font f, Color bg, Color fg, int right, int centerY, int padX, int h)
        {
            int tw = Measure(g, text, f);
            int w = tw + padX * 2;
            Rectangle r = new Rectangle(right - w, centerY - h / 2, w, h);
            Fill(g, r, h / 2, bg);
            TextC(g, text, f, r, fg);
            return w;
        }
    }

    // ==================================================================
    // 卡片容器
    // ==================================================================
    class Card : Panel
    {
        public string Title = "";
        public string Note = "";
        public int Step = 0;

        public Card(string title, int step, string note)
        {
            Title = title; Step = step; Note = note;
            BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 4 && Height > 4)
            {
                using (GraphicsPath p = Gfx.Round(new Rectangle(0, 0, Width, Height), Ui.Z(12)))
                    Region = new Region(p);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Gfx.Stroke(g, r, Ui.Z(12), T.Border);

            int tx = Ui.Z(22);
            if (Step > 0)
            {
                Rectangle cr = new Rectangle(Ui.Z(22), Ui.Z(17), Ui.Z(21), Ui.Z(21));
                Gfx.Fill(g, cr, Ui.Z(21), T.AccentSoft);
                Gfx.TextC(g, Step.ToString(), F.I.TinyBold, cr, T.Accent);
                tx = Ui.Z(51);
            }
            Gfx.TextL(g, Title, F.I.H2, new Rectangle(tx, Ui.Z(15), Ui.Z(400), Ui.Z(26)), T.Text);

            if (!string.IsNullOrEmpty(Note))
                Gfx.TextR(g, Note, F.I.Small, new Rectangle(0, Ui.Z(17), Width - Ui.Z(22), Ui.Z(22)), T.Muted);
        }
    }

    // ==================================================================
    // 输入框（圆角边框）
    // ==================================================================
    class InputBox : Panel
    {
        public TextBox Box = new TextBox();
        bool hot;

        public InputBox()
        {
            BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Color.White;
            Box.ForeColor = T.Text;
            Box.Font = F.I.Body;
            Controls.Add(Box);
            Box.GotFocus += delegate { hot = true; Invalidate(); };
            Box.LostFocus += delegate { hot = false; Invalidate(); };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Box != null)
            {
                int h = Box.PreferredHeight;
                Box.SetBounds(Ui.Z(13), (Height - h) / 2, Width - Ui.Z(26), h);
            }
            if (Width > 4 && Height > 4)
                using (GraphicsPath p = Gfx.Round(new Rectangle(0, 0, Width, Height), Ui.Z(8)))
                    Region = new Region(p);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(hot ? T.AccentSoft : Color.FromArgb(0xFA, 0xFB, 0xFD));
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Gfx.Stroke(g, r, Ui.Z(8), hot ? T.Accent : T.BorderStrong);
        }

        public override string Text
        {
            get { return Box.Text; }
            set { Box.Text = value; }
        }
    }

    // ==================================================================
    // 按钮
    // ==================================================================
    enum BtnKind { Primary, Secondary, Ghost, Danger }

    class Btn : Control
    {
        public BtnKind Kind;
        bool over, down;

        public Btn(string text, BtnKind kind)
        {
            Kind = kind;
            Text = text;
            Cursor = Cursors.Hand;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { over = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color bgParent = Parent != null ? Parent.BackColor : T.Bg;
            using (SolidBrush b = new SolidBrush(bgParent)) g.FillRectangle(b, ClientRectangle);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int rad = Ui.Z(8);
            Color fg = T.TextSoft;
            Font font = F.I.Btn;

            if (!Enabled)
            {
                Gfx.Fill(g, r, rad, Color.FromArgb(0xF4, 0xF6, 0xFA));
                Gfx.Stroke(g, r, rad, T.Border);
                fg = T.Muted;
            }
            else if (Kind == BtnKind.Primary)
            {
                Color c1 = down ? Color.FromArgb(0x25, 0x59, 0xDF) : (over ? Color.FromArgb(0x3D, 0x78, 0xFF) : T.Accent);
                Color c2 = down ? Color.FromArgb(0x33, 0x66, 0xEE) : (over ? Color.FromArgb(0x66, 0xA0, 0xFF) : T.Accent2);
                Gfx.FillGrad(g, r, rad, c1, c2);
                fg = Color.White;
                font = F.I.BtnBold;
            }
            else if (Kind == BtnKind.Danger)
            {
                Gfx.Fill(g, r, rad, over ? Color.FromArgb(0xFF, 0xF4, 0xF4) : Color.White);
                Gfx.Stroke(g, r, rad, over ? Color.FromArgb(0xF0, 0xB9, 0xB9) : Color.FromArgb(0xF2, 0xCB, 0xCB));
                fg = T.Red;
            }
            else if (Kind == BtnKind.Ghost)
            {
                if (over) Gfx.Fill(g, r, rad, Color.FromArgb(0xEF, 0xF4, 0xFD));
                fg = over ? T.Accent : T.Sub;
            }
            else
            {
                Gfx.Fill(g, r, rad, over ? Color.FromArgb(0xF7, 0xFA, 0xFF) : Color.White);
                Gfx.Stroke(g, r, rad, over ? Color.FromArgb(0xB9, 0xCB, 0xEC) : T.BorderStrong);
                fg = over ? T.Accent : T.TextSoft;
            }

            Rectangle tr = new Rectangle(0, down ? Ui.Z(1) : 0, Width, Height);
            Gfx.TextC(g, Text, font, tr, fg);
        }
    }

    // ==================================================================
    // 标题栏
    // ==================================================================
    class TitleBar : Control
    {
        readonly Form frm;
        Rectangle rMin, rClose;
        int hov;
        Image logo;

        public TitleBar(Form f)
        {
            frm = f;
            try { if (f.Icon != null) logo = f.Icon.ToBitmap(); }
            catch { }
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            LayoutButtons();
        }

        void LayoutButtons()
        {
            int w = Ui.Z(40), h = Ui.Z(30);
            rClose = new Rectangle(Width - Ui.Z(14) - w, Ui.Z(17), w, h);
            rMin = new Rectangle(rClose.X - Ui.Z(6) - w, Ui.Z(17), w, h);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutButtons(); Invalidate(); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = 0;
            if (rClose.Contains(e.Location)) h = 2;
            else if (rMin.Contains(e.Location)) h = 1;
            if (h != hov) { hov = h; Cursor = h == 0 ? Cursors.Default : Cursors.Hand; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hov = 0; Cursor = Cursors.Default; Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (rClose.Contains(e.Location)) { frm.Close(); return; }
            if (rMin.Contains(e.Location)) { frm.WindowState = FormWindowState.Minimized; return; }
            Native.ReleaseCapture();
            Native.SendMessage(frm.Handle, Native.WM_NCLBUTTONDOWN, new IntPtr(Native.HTCAPTION), IntPtr.Zero);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                Color.White, Color.FromArgb(0xFA, 0xFB, 0xFE), LinearGradientMode.Vertical))
                g.FillRectangle(lb, ClientRectangle);

            using (Pen pen = new Pen(T.Border)) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

            // logo
            Rectangle lg = new Rectangle(Ui.Z(19), Ui.Z(16), Ui.Z(32), Ui.Z(32));
            if (logo != null)
            {
                InterpolationMode old = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, lg);
                g.InterpolationMode = old;
            }
            else
            {
                Gfx.FillGrad(g, new Rectangle(Ui.Z(20), Ui.Z(17), Ui.Z(30), Ui.Z(30)), Ui.Z(9),
                    Color.FromArgb(0x3E, 0x7C, 0xFF), Color.FromArgb(0x28, 0x59, 0xE0));
                using (SolidBrush b = new SolidBrush(Color.White))
                    g.FillEllipse(b, Ui.Z(31), Ui.Z(26), Ui.Z(8), Ui.Z(8));
            }

            Gfx.TextLNoEllipsis(g, "模拟器串口配置", F.I.H1, new Rectangle(Ui.Z(60), Ui.Z(12), Ui.Z(320), Ui.Z(26)), T.Text);
            Gfx.TextLNoEllipsis(g, "把本机串口映射到雷电模拟器 UART", F.I.Small, new Rectangle(Ui.Z(61), Ui.Z(37), Ui.Z(360), Ui.Z(18)), T.Sub);

            // 窗口按钮
            if (hov == 1) Gfx.Fill(g, rMin, Ui.Z(7), Color.FromArgb(0xEC, 0xF1, 0xF9));
            using (Pen pen = new Pen(hov == 1 ? T.Text : T.Sub, 1.4f * Ui.S))
            {
                int cx = rMin.X + rMin.Width / 2, cy = rMin.Y + rMin.Height / 2;
                g.DrawLine(pen, cx - Ui.Z(6), cy, cx + Ui.Z(6), cy);
            }

            Color closeBg = hov == 2 ? Color.FromArgb(0xFD, 0xE7, 0xE7) : Color.White;
            if (hov == 2) Gfx.Fill(g, rClose, Ui.Z(7), closeBg);
            using (Pen pen = new Pen(hov == 2 ? T.Red : T.Sub, 1.4f * Ui.S))
            {
                int cx = rClose.X + rClose.Width / 2, cy = rClose.Y + rClose.Height / 2;
                int d = Ui.Z(5);
                g.DrawLine(pen, cx - d, cy - d, cx + d, cy + d);
                g.DrawLine(pen, cx - d, cy + d, cx + d, cy - d);
            }
        }
    }

    // ==================================================================
    // 胶囊选择器（模拟器实例）
    // ==================================================================
    class Pills : Control
    {
        public List<string> Items = new List<string>();
        public int Selected = -1;
        public event EventHandler SelectionChanged;
        int hov = -1;
        readonly List<Rectangle> rects = new List<Rectangle>();

        public Pills()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Default;
        }

        public void SetItems(List<string> items, int select)
        {
            Items = items;
            Selected = select;
            hov = -1;
            Invalidate();
        }

        public void SetSelectedSilently(int i) { Selected = i; Invalidate(); }

        void Rebuild(Graphics g)
        {
            rects.Clear();
            int x = 0;
            int h = Ui.Z(30);
            int y = (Height - h) / 2;
            for (int i = 0; i < Items.Count; i++)
            {
                int w = Gfx.Measure(g, Items[i], F.I.Body) + Ui.Z(30);
                rects.Add(new Rectangle(x, y, w, h));
                x += w + Ui.Z(8);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            using (Graphics g = CreateGraphics()) Rebuild(g);
            int h = -1;
            for (int i = 0; i < rects.Count; i++) if (rects[i].Contains(e.Location)) { h = i; break; }
            if (h != hov) { hov = h; Cursor = h >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e); hov = -1; Cursor = Cursors.Default; Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            using (Graphics g = CreateGraphics()) Rebuild(g);
            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i].Contains(e.Location) && i != Selected)
                {
                    Selected = i;
                    Invalidate();
                    if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (SolidBrush b = new SolidBrush(Parent != null ? Parent.BackColor : Color.White)) g.FillRectangle(b, ClientRectangle);

            Rebuild(g);
            if (Items.Count == 0)
            {
                Gfx.TextLNoEllipsis(g, "未找到模拟器实例", F.I.Body, new Rectangle(0, 0, Width, Height), T.Muted);
                return;
            }
            for (int i = 0; i < Items.Count; i++)
            {
                Rectangle r = rects[i];
                bool sel = i == Selected;
                if (sel)
                {
                    Gfx.Fill(g, r, r.Height / 2, T.AccentSoft);
                    Gfx.Ring(g, r, r.Height / 2, Color.FromArgb(0x9F, 0xBE, 0xFF), 1.2f * Ui.S);
                }
                else
                {
                    Gfx.Fill(g, r, r.Height / 2, i == hov ? Color.FromArgb(0xF6, 0xF9, 0xFF) : Color.White);
                    Gfx.Ring(g, r, r.Height / 2, i == hov ? Color.FromArgb(0xB9, 0xCB, 0xEC) : T.BorderStrong, 1f * Ui.S);
                }
                Gfx.TextC(g, Items[i], sel ? F.I.BodyBold : F.I.Body, r, sel ? T.Accent : T.TextSoft);
            }
        }
    }

    // ==================================================================
    // 列表容器（圆角边框 + 空状态）
    // ==================================================================
    class BufferedListBox : ListBox
    {
        public BufferedListBox() { DoubleBuffered = true; }
    }

    class ListPanel : Panel
    {
        public BufferedListBox List = new BufferedListBox();
        public Label Empty = new Label();

        public ListPanel()
        {
            BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            List.BorderStyle = BorderStyle.None;
            List.BackColor = Color.White;
            List.ForeColor = T.Text;
            List.Font = F.I.Body;
            List.DrawMode = DrawMode.OwnerDrawFixed;
            List.ItemHeight = Ui.Z(58);
            List.IntegralHeight = false;
            List.ScrollAlwaysVisible = false;
            List.TabStop = false;

            Empty.AutoSize = false;
            Empty.TextAlign = ContentAlignment.MiddleCenter;
            Empty.ForeColor = T.Muted;
            Empty.BackColor = Color.White;
            Empty.Font = F.I.Small;

            Controls.Add(List);
            Controls.Add(Empty);
            Empty.BringToFront();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (List != null)
            {
                List.SetBounds(Ui.Z(6), Ui.Z(6), Width - Ui.Z(12), Height - Ui.Z(12));
                Empty.SetBounds(Ui.Z(10), Ui.Z(10), Width - Ui.Z(20), Height - Ui.Z(20));
                Empty.BringToFront();
            }
            if (Width > 4 && Height > 4)
                using (GraphicsPath p = Gfx.Round(new Rectangle(0, 0, Width, Height), Ui.Z(10)))
                    Region = new Region(p);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            Gfx.Stroke(g, new Rectangle(0, 0, Width - 1, Height - 1), Ui.Z(10), T.Border);
        }
    }

    // ==================================================================
    // 原生方法
    // ==================================================================
    static class Native
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 2;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }

    // ==================================================================
    // 数据模型
    // ==================================================================
    class UartPort
    {
        public int Slot;
        public bool Enabled = true;
        public string IOBase = "0x2f8";
        public string IRQ = "3";
        public string Path = "";
        public string HostMode = "HostDevice";
        public string RawLine;      // 从文件里读到的原始 <Port .../> 行（未修改时原样写回）
    }

    class PortInfo
    {
        public string Name = "";
        public string Desc = "";
    }

    class PortRow
    {
        public PortInfo Info;
        public bool Mapped;
        public override string ToString() { return Info.Name; }
    }

    class MapRow
    {
        public UartPort Port;
        public override string ToString() { return Port.Path; }
    }

    // ==================================================================
    // leidian.vbox 读写
    // ==================================================================
    class VBoxModel
    {
        public string FilePath = "";
        public List<UartPort> Ports = new List<UartPort>();

        string raw = "";
        int innerStart;             // <UART> 与 </UART> 之间的字符区间
        int innerEnd;
        bool selfClosedUart;        // 原文件是 <UART/>
        string nl = "\r\n";
        string portPad = "        ";
        string closePad = "      ";
        string slash = "/>";
        string ioAttr = "IOBase";
        string irqAttr = "IRQ";
        Encoding enc = new UTF8Encoding(false);

        static int ParseInt(string s, int def)
        {
            int v;
            if (int.TryParse(s, out v)) return v;
            return def;
        }

        static string LeadingWs(string s)
        {
            int i = 0;
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
            return s.Substring(0, i);
        }

        public static VBoxModel Load(string path)
        {
            VBoxModel m = new VBoxModel();
            m.FilePath = path;

            byte[] bytes;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytes = new byte[fs.Length];
                int off = 0;
                while (off < bytes.Length)
                {
                    int n = fs.Read(bytes, off, bytes.Length - off);
                    if (n <= 0) break;
                    off += n;
                }
                if (off < bytes.Length) Array.Resize(ref bytes, off);
            }
            Encoding e;
            int skip = 0;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                e = new UTF8Encoding(true); skip = 3;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                e = new UnicodeEncoding(false, true); skip = 2;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                e = new UnicodeEncoding(true, true); skip = 2;
            }
            else
            {
                e = new UTF8Encoding(false);
            }
            m.enc = e;
            m.raw = e.GetString(bytes, skip, bytes.Length - skip);
            m.nl = m.raw.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";

            Match open = Regex.Match(m.raw, "<UART(\\s[^>]*)?>", RegexOptions.IgnoreCase);
            if (!open.Success)
                throw new InvalidDataException("配置文件中没有找到 <UART> 节点，这可能不是有效的雷电模拟器配置文件。");

            // <UART> 所在行的缩进
            int lineStart = m.raw.LastIndexOf('\n', open.Index);
            string uartIndent = lineStart < 0 ? "" : LeadingWs(m.raw.Substring(lineStart + 1, open.Index - lineStart - 1));
            m.closePad = uartIndent;

            if (open.Value.EndsWith("/>", StringComparison.Ordinal))
            {
                m.selfClosedUart = true;
                m.innerStart = m.innerEnd = open.Index + open.Length;
                m.portPad = uartIndent + "  ";
            }
            else
            {
                int closeIdx = m.raw.IndexOf("</UART>", open.Index + open.Length, StringComparison.OrdinalIgnoreCase);
                if (closeIdx < 0)
                    throw new InvalidDataException("配置文件中的 <UART> 节点没有正确闭合，文件可能已损坏。");
                m.innerStart = open.Index + open.Length;
                m.innerEnd = closeIdx;

                // </UART> 前的缩进
                int j = closeIdx - 1;
                while (j >= m.innerStart && (m.raw[j] == ' ' || m.raw[j] == '\t')) j--;
                if (j >= m.innerStart && m.raw[j] == '\n')
                    m.closePad = m.raw.Substring(j + 1, closeIdx - j - 1);
                else
                    m.closePad = uartIndent;
            }

            string inner = m.raw.Substring(m.innerStart, m.innerEnd - m.innerStart);
            bool firstPort = true;
            foreach (string ln0 in inner.Split('\n'))
            {
                string ln = ln0.TrimEnd('\r');
                string t = ln.Trim();
                if (t.Length == 0) continue;
                if (!t.StartsWith("<Port", StringComparison.OrdinalIgnoreCase)) continue;

                if (firstPort)
                {
                    m.portPad = LeadingWs(ln);
                    firstPort = false;
                }
                if (t.EndsWith(" />", StringComparison.Ordinal)) m.slash = " />";
                else if (t.EndsWith("/>", StringComparison.Ordinal)) m.slash = "/>";

                UartPort p = new UartPort();
                p.RawLine = t;

                foreach (Match a in Regex.Matches(t, "([A-Za-z_:][-A-Za-z0-9_:.]*)\\s*=\\s*\"([^\"]*)\""))
                {
                    string an = a.Groups[1].Value;
                    string av = a.Groups[2].Value;
                    string low = an.ToLowerInvariant();
                    if (low == "slot") p.Slot = ParseInt(av, 0);
                    else if (low == "enabled") p.Enabled = string.Equals(av, "true", StringComparison.OrdinalIgnoreCase);
                    else if (low == "iobase") { p.IOBase = av; m.ioAttr = an; }
                    else if (low == "irq") { p.IRQ = av; m.irqAttr = an; }
                    else if (low == "path") p.Path = av;
                    else if (low == "hostmode") p.HostMode = av;
                }
                m.Ports.Add(p);
            }

            if (firstPort) m.portPad = m.closePad + "  ";
            if (m.portPad.Length == 0) m.portPad = m.closePad + "  ";
            return m;
        }

        string NewPortLine(UartPort p)
        {
            return "<Port slot=\"" + p.Slot.ToString() + "\" enabled=\"" + (p.Enabled ? "true" : "false") + "\" "
                + ioAttr + "=\"" + p.IOBase + "\" " + irqAttr + "=\"" + p.IRQ + "\" path=\"" + p.Path + "\" hostMode=\"" + p.HostMode + "\"" + slash;
        }

        public string Build()
        {
            StringBuilder sb = new StringBuilder();
            string head = raw.Substring(0, innerStart);
            if (selfClosedUart) head = head.Substring(0, head.Length - 2) + ">";  // <UART/> -> <UART>
            sb.Append(head);

            foreach (UartPort p in Ports.OrderBy(delegate (UartPort x) { return x.Slot; }))
            {
                sb.Append(nl);
                sb.Append(portPad);
                sb.Append(p.RawLine != null ? p.RawLine : NewPortLine(p));
            }
            sb.Append(nl);
            sb.Append(closePad);
            if (selfClosedUart) sb.Append("</UART>");
            sb.Append(raw.Substring(innerEnd));
            return sb.ToString();
        }

        public static bool IsReadOnly(string path)
        {
            try
            {
                FileAttributes a = File.GetAttributes(path);
                return (a & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            }
            catch { return false; }
        }

        public bool FileReadOnly { get { return IsReadOnly(FilePath); } }

        public void Save()
        {
            string text = Build();

            // 写前校验：必须是合法 XML
            XmlDocument chk = new XmlDocument();
            chk.XmlResolver = null;
            chk.LoadXml(text);

            try { File.Copy(FilePath, FilePath + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); }
            catch { }

            // 只读的文件无法直接写入：先临时去掉只读属性
            bool wasReadOnly = IsReadOnly(FilePath);
            if (wasReadOnly)
                File.SetAttributes(FilePath, File.GetAttributes(FilePath) & ~FileAttributes.ReadOnly);

            try
            {
                File.WriteAllText(FilePath, text, enc);
            }
            finally
            {
                // 改完立刻设为只读，防止启动模拟器时该文件被覆盖
                try
                {
                    File.SetAttributes(FilePath, File.GetAttributes(FilePath) | FileAttributes.ReadOnly);
                }
                catch { }
            }
        }
    }

    // ==================================================================
    // 雷电模拟器进程控制
    // ==================================================================
    static class Ld
    {
        static readonly string[] ProcNames = new string[] { "dnplayer", "LdVBoxHeadless", "Ld9BoxHeadless", "dnmultiplayer" };

        public static bool IsRunning()
        {
            foreach (string n in ProcNames)
            {
                try { if (Process.GetProcessesByName(n).Length > 0) return true; }
                catch { }
            }
            return false;
        }

        public static void KillAll()
        {
            foreach (string n in ProcNames)
            {
                Process[] ps = new Process[0];
                try { ps = Process.GetProcessesByName(n); } catch { }
                foreach (Process p in ps)
                {
                    try { p.Kill(); } catch { }
                }
                foreach (Process p in ps)
                {
                    try { p.WaitForExit(4000); } catch { }
                    try { p.Dispose(); } catch { }
                }
            }
            Thread.Sleep(600);
        }

        public static bool Start(string dir, int index)
        {
            string exe = Path.Combine(dir, "dnplayer.exe");
            if (!File.Exists(exe)) return false;
            ProcessStartInfo psi = new ProcessStartInfo(exe);
            psi.WorkingDirectory = dir;
            psi.UseShellExecute = true;
            if (index > 0) psi.Arguments = "index=" + index.ToString();
            try { Process.Start(psi); return true; }
            catch { return false; }
        }
    }

    // ==================================================================
    // 主窗口
    // ==================================================================
    public class MainForm : Form
    {
        const string APP = "模拟器串口配置";

        // 布局（96dpi 设计坐标）
        const int DW = 980, DH = 700;

        TitleBar titleBar;
        Card cardEnv, cardMap;
        Label lblDir, lblInst, lblVbox;
        InputBox inputDir;
        Btn btnBrowse, btnRefresh, btnReload;
        Pills pills;

        Label lblPorts, lblMaps, lblMapNote;
        ListPanel portPanel, mapPanel;
        BufferedListBox lstPorts, lstMaps;
        Btn btnAdd, btnRemove, btnClear;
        Btn btnSave, btnClose;
        Label lblStatus;

        // 数据
        string installDir = "";
        List<string> instances = new List<string>();
        int curInstance = -1;
        VBoxModel model;
        List<PortInfo> phys = new List<PortInfo>();
        bool dirty = false;
        int hoverPort = -1, hoverMap = -1;
        bool booted = false;
        public string ForceDir = "";

        static readonly string[] SlotIO = new string[] { "0x3f8", "0x2f8", "0x3e8", "0x2e8" };
        static readonly string[] SlotIRQ = new string[] { "4", "3", "4", "3" };

        public MainForm()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Ui.S = g.DpiX / 96f;
            if (Ui.S < 1f) Ui.S = 1f;
            // 屏幕放不下时整体等比缩小
            try
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                float maxS = Math.Min((wa.Width - 40) / (float)DW, (wa.Height - 60) / (float)DH);
                if (maxS > 0.6f && Ui.S > maxS) Ui.S = maxS;
            }
            catch { }
            F.I = new Fonts(Ui.S);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = T.Bg;
            Text = APP;
            Font = F.I.Body;
            KeyPreview = true;
            DoubleBuffered = true;
            ClientSize = new Size(Ui.Z(DW), Ui.Z(DH));
            MinimumSize = new Size(Ui.Z(DW), Ui.Z(DH));

            BuildUi();
            UpdateRegion();

            Load += delegate { if (!booted) { booted = true; Boot(); } };
            FormClosing += OnClosing;
            KeyDown += OnKeyDown;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        void UpdateRegion()
        {
            if (Width > 4 && Height > 4)
                using (GraphicsPath p = Gfx.Round(new Rectangle(0, 0, Width, Height), Ui.Z(14)))
                    Region = new Region(p);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateRegion(); }

        // ------------------------------------------------------------
        // 界面构建
        // ------------------------------------------------------------
        void BuildUi()
        {
            SuspendLayout();

            titleBar = new TitleBar(this);
            titleBar.SetBounds(0, 0, Ui.Z(DW), Ui.Z(64));
            Controls.Add(titleBar);

            // ---------- 卡片 1 ----------
            cardEnv = new Card("选择模拟器", 1, "雷电 9 / 雷电 14 等 VirtualBox 内核版本");
            cardEnv.SetBounds(Ui.Z(24), Ui.Z(80), Ui.Z(932), Ui.Z(180));
            Controls.Add(cardEnv);

            lblDir = new Label();
            lblDir.AutoSize = false;
            lblDir.TextAlign = ContentAlignment.MiddleLeft;
            lblDir.ForeColor = T.TextSoft;
            lblDir.Font = F.I.Body;
            lblDir.Text = "安装目录";
            lblDir.SetBounds(Ui.Z(22), Ui.Z(50), Ui.Z(80), Ui.Z(40));
            cardEnv.Controls.Add(lblDir);

            inputDir = new InputBox();
            inputDir.SetBounds(Ui.Z(108), Ui.Z(50), Ui.Z(690), Ui.Z(40));
            cardEnv.Controls.Add(inputDir);
            inputDir.Box.TextChanged += delegate { };

            btnBrowse = new Btn("浏览…", BtnKind.Secondary);
            btnBrowse.SetBounds(Ui.Z(812), Ui.Z(50), Ui.Z(98), Ui.Z(40));
            btnBrowse.Click += delegate { BrowseDir(); };
            cardEnv.Controls.Add(btnBrowse);

            lblInst = new Label();
            lblInst.AutoSize = false;
            lblInst.TextAlign = ContentAlignment.MiddleLeft;
            lblInst.ForeColor = T.TextSoft;
            lblInst.Font = F.I.Body;
            lblInst.Text = "实例";
            lblInst.SetBounds(Ui.Z(22), Ui.Z(104), Ui.Z(80), Ui.Z(34));
            cardEnv.Controls.Add(lblInst);

            pills = new Pills();
            pills.SetBounds(Ui.Z(108), Ui.Z(102), Ui.Z(690), Ui.Z(36));
            pills.SelectionChanged += delegate { OnInstanceChanged(); };
            cardEnv.Controls.Add(pills);

            btnRefresh = new Btn("刷新", BtnKind.Secondary);
            btnRefresh.SetBounds(Ui.Z(812), Ui.Z(104), Ui.Z(98), Ui.Z(34));
            btnRefresh.Click += delegate { RefreshInstances(true); };
            cardEnv.Controls.Add(btnRefresh);

            lblVbox = new Label();
            lblVbox.AutoSize = false;
            lblVbox.AutoEllipsis = true;
            lblVbox.TextAlign = ContentAlignment.MiddleLeft;
            lblVbox.ForeColor = T.Sub;
            lblVbox.Font = F.I.Small;
            lblVbox.Text = "配置文件：未选择";
            lblVbox.SetBounds(Ui.Z(22), Ui.Z(146), Ui.Z(888), Ui.Z(20));
            cardEnv.Controls.Add(lblVbox);

            // ---------- 卡片 2 ----------
            cardMap = new Card("串口映射", 2, "slot 0 已被系统日志占用，其余 3 个槽位可自由映射");
            cardMap.SetBounds(Ui.Z(24), Ui.Z(276), Ui.Z(932), Ui.Z(328));
            Controls.Add(cardMap);

            lblPorts = new Label();
            lblPorts.AutoSize = false;
            lblPorts.TextAlign = ContentAlignment.MiddleLeft;
            lblPorts.ForeColor = T.Text;
            lblPorts.Font = F.I.SmallBold;
            lblPorts.Text = "本机串口";
            lblPorts.SetBounds(Ui.Z(22), Ui.Z(54), Ui.Z(160), Ui.Z(22));
            cardMap.Controls.Add(lblPorts);

            btnClear = new Btn("刷新", BtnKind.Secondary);
            btnClear.SetBounds(Ui.Z(352), Ui.Z(50), Ui.Z(70), Ui.Z(26));
            btnClear.Click += delegate { LoadPortsAsync(); };
            cardMap.Controls.Add(btnClear);

            portPanel = new ListPanel();
            portPanel.SetBounds(Ui.Z(22), Ui.Z(84), Ui.Z(400), Ui.Z(224));
            cardMap.Controls.Add(portPanel);
            lstPorts = portPanel.List;
            portPanel.Empty.Text = "未检测到可用串口\r\n请连接 USB 转串口设备后点击「刷新」";
            lstPorts.DrawItem += DrawPortItem;
            lstPorts.MouseMove += delegate { TrackHover(lstPorts, ref hoverPort); };
            lstPorts.MouseLeave += delegate { hoverPort = -1; lstPorts.Invalidate(); };

            btnAdd = new Btn("添加 →", BtnKind.Primary);
            btnAdd.SetBounds(Ui.Z(438), Ui.Z(148), Ui.Z(96), Ui.Z(40));
            btnAdd.Click += delegate { AddMapping(); };
            cardMap.Controls.Add(btnAdd);

            btnRemove = new Btn("← 删除", BtnKind.Danger);
            btnRemove.SetBounds(Ui.Z(438), Ui.Z(198), Ui.Z(96), Ui.Z(40));
            btnRemove.Click += delegate { RemoveMapping(); };
            cardMap.Controls.Add(btnRemove);

            lblMaps = new Label();
            lblMaps.AutoSize = false;
            lblMaps.TextAlign = ContentAlignment.MiddleLeft;
            lblMaps.ForeColor = T.Text;
            lblMaps.Font = F.I.SmallBold;
            lblMaps.Text = "模拟器串口（已配置）";
            lblMaps.SetBounds(Ui.Z(550), Ui.Z(54), Ui.Z(220), Ui.Z(22));
            cardMap.Controls.Add(lblMaps);

            lblMapNote = new Label();
            lblMapNote.AutoSize = false;
            lblMapNote.TextAlign = ContentAlignment.MiddleRight;
            lblMapNote.ForeColor = T.Muted;
            lblMapNote.Font = F.I.Small;
            lblMapNote.Text = "保存后生效";
            lblMapNote.SetBounds(Ui.Z(700), Ui.Z(54), Ui.Z(210), Ui.Z(22));
            cardMap.Controls.Add(lblMapNote);

            mapPanel = new ListPanel();
            mapPanel.SetBounds(Ui.Z(550), Ui.Z(84), Ui.Z(360), Ui.Z(224));
            cardMap.Controls.Add(mapPanel);
            lstMaps = mapPanel.List;
            mapPanel.Empty.Text = "还没有串口映射\r\n在左侧选中串口后点击「添加 →」";
            lstMaps.DrawItem += DrawMapItem;
            lstMaps.MouseMove += delegate { TrackHover(lstMaps, ref hoverMap); };
            lstMaps.MouseLeave += delegate { hoverMap = -1; lstMaps.Invalidate(); };

            // ---------- 底部 ----------
            lblStatus = new Label();
            lblStatus.AutoSize = false;
            lblStatus.AutoEllipsis = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.ForeColor = T.Sub;
            lblStatus.Font = F.I.Small;
            lblStatus.Text = "提示：保存前请先关闭模拟器，修改在模拟器下次启动时生效。";
            lblStatus.SetBounds(Ui.Z(26), Ui.Z(644), Ui.Z(530), Ui.Z(24));
            Controls.Add(lblStatus);

            btnReload = new Btn("重新加载", BtnKind.Ghost);
            btnReload.SetBounds(Ui.Z(574), Ui.Z(632), Ui.Z(104), Ui.Z(44));
            btnReload.Click += delegate { ReloadCurrent(); };
            Controls.Add(btnReload);

            btnClose = new Btn("关闭", BtnKind.Ghost);
            btnClose.SetBounds(Ui.Z(690), Ui.Z(632), Ui.Z(104), Ui.Z(44));
            btnClose.Click += delegate { Close(); };
            Controls.Add(btnClose);

            btnSave = new Btn("保存配置", BtnKind.Primary);
            btnSave.SetBounds(Ui.Z(806), Ui.Z(632), Ui.Z(150), Ui.Z(44));
            btnSave.Click += delegate { Save(); };
            Controls.Add(btnSave);

            ResumeLayout();
        }

        // ------------------------------------------------------------
        // 启动
        // ------------------------------------------------------------
        void Boot()
        {
            string d = !string.IsNullOrEmpty(ForceDir) && Directory.Exists(ForceDir) ? ForceDir : DetectInstallDir();
            if (!string.IsNullOrEmpty(d))
            {
                SetInstallDir(d, true);
            }
            else
            {
                SetStatus("请点击「浏览…」选择雷电模拟器安装目录（例如 D:\\LDPlayer14）", false);
                RefreshInstances(false);
            }
            LoadPortsAsync();
        }

        void SetStatus(string msg, bool ok)
        {
            lblStatus.Text = msg;
            lblStatus.ForeColor = ok ? T.Green : T.Sub;
        }

        static string NormalizeDir(string s)
        {
            if (s == null) return "";
            s = s.Trim().Trim('"');
            if (s.Length == 0) return "";
            try { s = Path.GetFullPath(s); } catch { }
            return s.TrimEnd('\\', '/');
        }

        void SetInstallDir(string dir, bool auto)
        {
            dir = NormalizeDir(dir);
            installDir = dir;
            inputDir.Text = dir;
            RefreshInstances(auto);
        }

        void BrowseDir()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "请选择雷电模拟器的安装目录（目录内应有 dnplayer.exe）";
                dlg.ShowNewFolderButton = false;
                string cur = NormalizeDir(inputDir.Text);
                if (cur.Length > 0 && Directory.Exists(cur)) dlg.SelectedPath = cur;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SetInstallDir(dlg.SelectedPath, false);
                }
            }
        }

        void RefreshInstances(bool interactive)
        {
            List<string> found = new List<string>();
            string dir = NormalizeDir(inputDir.Text);

            if (dir.Length == 0)
            {
                if (interactive) MessageBox.Show(this, "请先选择雷电模拟器的安装目录。", APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                instances = found;
                pills.SetItems(new List<string>(), -1);
                SetModel(null, "");
                return;
            }

            if (!Directory.Exists(dir))
            {
                if (interactive) MessageBox.Show(this, "目录不存在：" + dir, APP, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                instances = found;
                pills.SetItems(new List<string>(), -1);
                SetModel(null, "");
                return;
            }

            string vms = Path.Combine(dir, "vms");
            if (Directory.Exists(vms))
            {
                string[] ds = Directory.GetDirectories(vms, "leidian*");
                List<KeyValuePair<int, string>> tmp = new List<KeyValuePair<int, string>>();
                foreach (string d in ds)
                {
                    if (!File.Exists(Path.Combine(d, "leidian.vbox"))) continue;
                    string name = Path.GetFileName(d);
                    int idx = 9999;
                    Match mm = Regex.Match(name, "(\\d+)$");
                    if (mm.Success) int.TryParse(mm.Groups[1].Value, out idx);
                    tmp.Add(new KeyValuePair<int, string>(idx, name));
                }
                tmp.Sort(delegate (KeyValuePair<int, string> a, KeyValuePair<int, string> b)
                {
                    if (a.Key != b.Key) return a.Key.CompareTo(b.Key);
                    return string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase);
                });
                foreach (KeyValuePair<int, string> kv in tmp) found.Add(kv.Value);
            }

            instances = found;

            if (found.Count == 0)
            {
                pills.SetItems(new List<string>(), -1);
                curInstance = -1;
                SetModel(null, "");
                lblVbox.ForeColor = T.Amber;
                lblVbox.Text = "未找到 vms\\leidian0\\leidian.vbox —— 请先启动一次模拟器，然后点击「刷新」";
                if (interactive)
                    MessageBox.Show(this,
                        "没有找到 leidian.vbox 配置文件。\r\n\r\n请先启动一次模拟器（首次启动会在安装目录的 vms\\leidian0\\ 下生成 leidian.vbox），\r\n然后再点击「刷新」。",
                        APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (found.Count > 1)
                SetStatus("检测到 " + found.Count + " 个模拟器实例，请在上方选择要配置的实例。", false);

            pills.SetItems(found, 0);
            curInstance = 0;
            LoadVbox(found[0]);
        }

        void OnInstanceChanged()
        {
            int idx = pills.Selected;
            if (idx < 0 || idx >= instances.Count) return;
            if (dirty)
            {
                DialogResult r = MessageBox.Show(this, "当前实例有未保存的修改，切换后修改会丢失。\r\n\r\n确定要切换吗？",
                    APP, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes)
                {
                    pills.SetSelectedSilently(curInstance);
                    return;
                }
                dirty = false;
            }
            curInstance = idx;
            LoadVbox(instances[idx]);
        }

        void SetModel(VBoxModel m, string path)
        {
            model = m;
            if (m == null)
            {
                if (path.Length == 0) lblVbox.Text = "配置文件：未选择";
            }
            RefreshMapList();
            RefreshPortList();
        }

        void LoadVbox(string inst)
        {
            string p = Path.Combine(new string[] { NormalizeDir(inputDir.Text), "vms", inst, "leidian.vbox" });
            if (!File.Exists(p))
            {
                MessageBox.Show(this, "配置文件不存在：\r\n" + p, APP, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetModel(null, "");
                return;
            }
            try
            {
                VBoxModel m = VBoxModel.Load(p);
                dirty = false;
                SetModel(m, p);
                bool ro = VBoxModel.IsReadOnly(p);
                lblVbox.ForeColor = ro ? T.Green : T.Sub;
                lblVbox.Text = (ro ? "配置文件（只读保护）：" : "配置文件：") + p;
                int n = m.Ports.Count;
                SetStatus("已加载 " + inst + "，当前有 " + n.ToString() + " 个串口配置。" + (ro ? "文件为只读状态，保存时会自动解除后再加回。" : ""), true);
            }
            catch (Exception ex)
            {
                SetModel(null, "");
                lblVbox.ForeColor = T.Red;
                lblVbox.Text = "读取失败：" + p;
                MessageBox.Show(this, "读取配置文件失败：\r\n" + ex.Message, APP, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ReloadCurrent()
        {
            if (instances.Count == 0 || curInstance < 0) { RefreshInstances(false); return; }
            if (dirty)
            {
                if (MessageBox.Show(this, "放弃未保存的修改并重新加载？", APP, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            LoadVbox(instances[curInstance]);
        }

        // ------------------------------------------------------------
        // 串口枚举
        // ------------------------------------------------------------
        void LoadPortsAsync()
        {
            SetStatus("正在读取本机串口…", false);
            ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                List<PortInfo> list = EnumPorts();
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        phys = list;
                        RefreshPortList();
                        SetStatus(list.Count == 0 ? "未检测到可用串口。可连接 USB 转串口设备后点击「刷新」。" : "已检测到 " + list.Count.ToString() + " 个串口。", list.Count > 0);
                    });
                }
                catch { }
            });
        }

        static List<PortInfo> EnumPorts()
        {
            Dictionary<string, string> desc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Caption FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'"))
                {
                    foreach (ManagementBaseObject mo in searcher.Get())
                    {
                        object capObj = mo["Caption"];
                        string cap = capObj == null ? "" : capObj.ToString();
                        Match m = Regex.Match(cap, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
                        if (!m.Success) continue;
                        string nm = m.Groups[1].Value.ToUpperInvariant();
                        string d = cap.Replace("(" + m.Groups[1].Value + ")", "").Trim();
                        if (d.Length == 0) d = "串口设备";
                        if (!desc.ContainsKey(nm)) desc[nm] = d;
                    }
                }
            }
            catch { }

            List<PortInfo> list = new List<PortInfo>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
                {
                    if (k != null)
                    {
                        foreach (string vn in k.GetValueNames())
                        {
                            object val = k.GetValue(vn);
                            string com = val == null ? "" : val.ToString().Trim();
                            if (com.Length == 0 || !seen.Add(com)) continue;
                            PortInfo pi = new PortInfo();
                            pi.Name = com.ToUpperInvariant();
                            string d;
                            pi.Desc = desc.TryGetValue(pi.Name, out d) ? d : "串口设备（" + vn + "）";
                            list.Add(pi);
                        }
                    }
                }
            }
            catch { }

            foreach (KeyValuePair<string, string> kv in desc)
            {
                if (seen.Contains(kv.Key)) continue;
                PortInfo pi = new PortInfo();
                pi.Name = kv.Key;
                pi.Desc = kv.Value;
                list.Add(pi);
            }

            list.Sort(delegate (PortInfo a, PortInfo b)
            {
                int ia = 0, ib = 0;
                Match ma = Regex.Match(a.Name, "(\\d+)");
                Match mb = Regex.Match(b.Name, "(\\d+)");
                if (ma.Success) int.TryParse(ma.Groups[1].Value, out ia);
                if (mb.Success) int.TryParse(mb.Groups[1].Value, out ib);
                if (ia != ib) return ia.CompareTo(ib);
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        // ------------------------------------------------------------
        // 列表刷新
        // ------------------------------------------------------------
        bool IsMapped(string com)
        {
            if (model == null) return false;
            foreach (UartPort p in model.Ports)
                if (string.Equals((p.Path ?? "").Trim(), com, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        void RefreshPortList()
        {
            object sel = lstPorts.SelectedItem;
            string selName = sel is PortRow ? ((PortRow)sel).Info.Name : null;

            lstPorts.BeginUpdate();
            lstPorts.Items.Clear();
            foreach (PortInfo pi in phys)
            {
                PortRow r = new PortRow();
                r.Info = pi;
                r.Mapped = IsMapped(pi.Name);
                lstPorts.Items.Add(r);
            }
            if (selName != null)
            {
                for (int i = 0; i < lstPorts.Items.Count; i++)
                {
                    PortRow r = lstPorts.Items[i] as PortRow;
                    if (r != null && string.Equals(r.Info.Name, selName, StringComparison.OrdinalIgnoreCase)) { lstPorts.SelectedIndex = i; break; }
                }
            }
            lstPorts.EndUpdate();

            portPanel.Empty.Visible = lstPorts.Items.Count == 0;
            lstPorts.Visible = lstPorts.Items.Count > 0;
            if (portPanel.Empty.Visible) portPanel.Empty.BringToFront();
        }

        void RefreshMapList()
        {
            lstMaps.BeginUpdate();
            lstMaps.Items.Clear();
            if (model != null)
            {
                foreach (UartPort p in model.Ports.OrderBy(delegate (UartPort x) { return x.Slot; }))
                {
                    MapRow r = new MapRow();
                    r.Port = p;
                    lstMaps.Items.Add(r);
                }
            }
            lstMaps.EndUpdate();

            mapPanel.Empty.Visible = lstMaps.Items.Count == 0;
            lstMaps.Visible = lstMaps.Items.Count > 0;
            if (mapPanel.Empty.Visible) mapPanel.Empty.BringToFront();

            lstPorts.Invalidate();
        }

        void TrackHover(ListBox lb, ref int hover)
        {
            int i = lb.IndexFromPoint(lb.PointToClient(Cursor.Position));
            if (i != hover) { hover = i; lb.Invalidate(); }
        }

        // ------------------------------------------------------------
        // 绘制列表项
        // ------------------------------------------------------------
        void DrawPortItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstPorts.Items.Count) return;
            PortRow row = lstPorts.Items[e.Index] as PortRow;
            if (row == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle r = new Rectangle(e.Bounds.X + Ui.Z(3), e.Bounds.Y + Ui.Z(3), e.Bounds.Width - Ui.Z(6), e.Bounds.Height - Ui.Z(6));
            bool sel = (e.State & DrawItemState.Selected) != 0;

            using (SolidBrush b = new SolidBrush(Color.White)) g.FillRectangle(b, e.Bounds);
            if (sel) Gfx.Fill(g, r, Ui.Z(9), T.AccentSoft);
            else if (hoverPort == e.Index) Gfx.Fill(g, r, Ui.Z(9), Color.FromArgb(0xF6, 0xF9, 0xFF));

            int cy = r.Y + r.Height / 2;
            Color dot = row.Mapped ? T.Accent : T.Green;
            using (SolidBrush b = new SolidBrush(dot))
                g.FillEllipse(b, r.X + Ui.Z(12), cy - Ui.Z(4), Ui.Z(8), Ui.Z(8));

            int textX = r.X + Ui.Z(30);
            int badgeW = row.Mapped ? Gfx.Measure(g, "已映射", F.I.Tiny) + Ui.Z(16) : Ui.Z(8);
            int textW = r.Width - Ui.Z(30) - badgeW - Ui.Z(10);

            Gfx.TextL(g, row.Info.Name, F.I.RowTitle, new Rectangle(textX, r.Y + Ui.Z(9), textW, Ui.Z(22)), sel ? T.Accent : T.Text);
            Gfx.TextL(g, row.Info.Desc, F.I.RowSub, new Rectangle(textX, r.Y + Ui.Z(31), textW, Ui.Z(18)), T.Sub);

            if (row.Mapped)
            {
                Gfx.Badge(g, "已映射", F.I.Tiny, Color.FromArgb(0xEE, 0xF2, 0xF9), Color.FromArgb(0x77, 0x84, 0x9B),
                    r.Right - Ui.Z(12), cy, Ui.Z(8), Ui.Z(20));
            }
        }

        void DrawMapItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstMaps.Items.Count) return;
            MapRow row = lstMaps.Items[e.Index] as MapRow;
            if (row == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle r = new Rectangle(e.Bounds.X + Ui.Z(3), e.Bounds.Y + Ui.Z(3), e.Bounds.Width - Ui.Z(6), e.Bounds.Height - Ui.Z(6));
            bool sel = (e.State & DrawItemState.Selected) != 0;

            using (SolidBrush b = new SolidBrush(Color.White)) g.FillRectangle(b, e.Bounds);
            if (sel) Gfx.Fill(g, r, Ui.Z(9), T.AccentSoft);
            else if (hoverMap == e.Index) Gfx.Fill(g, r, Ui.Z(9), Color.FromArgb(0xF6, 0xF9, 0xFF));

            bool raw = string.Equals(row.Port.HostMode, "RawFile", StringComparison.OrdinalIgnoreCase);
            int cy = r.Y + r.Height / 2;

            Rectangle tag = new Rectangle(r.X + Ui.Z(10), cy - Ui.Z(11), Ui.Z(44), Ui.Z(22));
            Gfx.Fill(g, tag, Ui.Z(6), raw ? Color.FromArgb(0xF1, 0xF4, 0xF9) : T.AccentSoft);
            Gfx.TextC(g, "slot " + row.Port.Slot.ToString(), F.I.TinyBold, tag, raw ? T.Sub : T.Accent);

            string badgeText = raw ? "日志" : (row.Port.Enabled ? "启用" : "停用");
            Color badgeBg = raw ? Color.FromArgb(0xF1, 0xF4, 0xF9) : (row.Port.Enabled ? T.GreenSoft : Color.FromArgb(0xF4, 0xF5, 0xF8));
            Color badgeFg = raw ? T.Muted : (row.Port.Enabled ? T.Green : T.Muted);
            int badgeW = Gfx.Measure(g, badgeText, F.I.Tiny) + Ui.Z(14);

            int textX = tag.Right + Ui.Z(10);
            int titleW = r.Right - textX - badgeW - Ui.Z(16);
            int subW = r.Right - textX - Ui.Z(10);

            Gfx.TextLPath(g, row.Port.Path, F.I.RowTitle, new Rectangle(textX, r.Y + Ui.Z(9), titleW, Ui.Z(22)), T.Text);

            string sub = row.Port.IOBase + " · IRQ " + row.Port.IRQ + " · " + (row.Port.Enabled ? row.Port.HostMode : "未启用");
            Gfx.TextL(g, sub, F.I.RowSub, new Rectangle(textX, r.Y + Ui.Z(31), subW, Ui.Z(18)), T.Sub);

            Gfx.Badge(g, badgeText, F.I.Tiny, badgeBg, badgeFg, r.Right - Ui.Z(10), r.Y + Ui.Z(20), Ui.Z(7), Ui.Z(18));
        }

        // ------------------------------------------------------------
        // 增删映射
        // ------------------------------------------------------------
        void AddMapping()
        {
            if (model == null)
            {
                MessageBox.Show(this, "请先选择模拟器安装目录和实例。", APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            PortRow row = lstPorts.SelectedItem as PortRow;
            if (row == null)
            {
                MessageBox.Show(this, "请先在左侧「本机串口」中选择一个串口。", APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (IsMapped(row.Info.Name))
            {
                MessageBox.Show(this, "串口 " + row.Info.Name + " 已经在映射列表中了。", APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool[] used = new bool[4];
            foreach (UartPort p in model.Ports) if (p.Slot >= 0 && p.Slot < 4) used[p.Slot] = true;

            int slot = -1;
            for (int i = 1; i <= 3; i++) if (!used[i]) { slot = i; break; }
            if (slot < 0)
            {
                MessageBox.Show(this, "已达到上限：一个实例最多 4 个串口（slot 0 已被系统日志占用）。\r\n\r\n请先删除一个已有的映射。",
                    APP, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UartPort np = new UartPort();
            np.Slot = slot;
            np.Enabled = true;
            np.IOBase = SlotIO[slot];
            np.IRQ = SlotIRQ[slot];
            np.Path = row.Info.Name;
            np.HostMode = "HostDevice";
            model.Ports.Add(np);
            dirty = true;

            RefreshMapList();
            RefreshPortList();
            for (int i = 0; i < lstMaps.Items.Count; i++)
            {
                MapRow mr = lstMaps.Items[i] as MapRow;
                if (mr != null && mr.Port == np) { lstMaps.SelectedIndex = i; break; }
            }
            SetStatus("已添加 " + row.Info.Name + " → slot " + slot.ToString() + "（尚未保存）", false);
        }

        void RemoveMapping()
        {
            if (model == null) return;
            MapRow row = lstMaps.SelectedItem as MapRow;
            if (row == null)
            {
                MessageBox.Show(this, "请先在右侧列表中选择要删除的串口映射。", APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (row.Port.Slot == 0)
            {
                MessageBox.Show(this, "slot 0 是模拟器的系统日志端口，不能删除。", APP, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            model.Ports.Remove(row.Port);
            dirty = true;
            RefreshMapList();
            RefreshPortList();
            SetStatus("已删除 slot " + row.Port.Slot.ToString() + " 的映射（尚未保存）", false);
        }

        // ------------------------------------------------------------
        // 保存
        // ------------------------------------------------------------
        void Save()
        {
            if (model == null)
            {
                MessageBox.Show(this, "还没有加载任何配置文件。\r\n请先选择模拟器安装目录和实例。", APP, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (instances.Count == 0 || curInstance < 0) return;

            // 保存前先判断是不是只读：只读文件直接写入会失败，这里先记下来
            bool wasReadOnly = VBoxModel.IsReadOnly(model.FilePath);
            if (wasReadOnly) SetStatus("配置文件当前为只读，保存时会临时解除只读后再写入…", false);

            if (Ld.IsRunning())
            {
                DialogResult r = MessageBox.Show(this,
                    "检测到模拟器正在运行。\r\n\r\n模拟器退出时可能会覆盖配置文件，因此需要先关闭模拟器再保存。\r\n（会关闭所有正在运行的雷电模拟器实例）\r\n\r\n现在关闭模拟器并保存吗？",
                    APP, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
                SetStatus("正在关闭模拟器…", false);
                Application.DoEvents();
                Ld.KillAll();
            }

            try
            {
                model.Save();
                dirty = false;
                lblVbox.ForeColor = T.Green;
                lblVbox.Text = "配置文件（只读保护）：" + model.FilePath;
                SetStatus("配置已保存，并已设为只读 —— 启动模拟器时不会再覆盖该文件。", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "写入配置文件失败：\r\n" + ex.Message + "\r\n\r\n请确认模拟器已完全退出（可在任务管理器中结束 dnplayer.exe / LdVBoxHeadless.exe），"
                    + "并检查该文件是否被其他程序占用或所在磁盘是否可写。",
                    APP, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int idx = 0;
            Match mm = Regex.Match(instances[curInstance], "(\\d+)$");
            if (mm.Success) int.TryParse(mm.Groups[1].Value, out idx);

            DialogResult go = MessageBox.Show(this,
                "配置已保存到：\r\n" + model.FilePath + "\r\n\r\n"
                + "该文件已设为「只读」，启动模拟器时不会再被覆盖。\r\n\r\n"
                + "是否立即启动 / 重启模拟器使配置生效？",
                APP, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (go == DialogResult.Yes)
            {
                if (Ld.IsRunning()) Ld.KillAll();
                if (Ld.Start(NormalizeDir(inputDir.Text), idx))
                    SetStatus("正在启动模拟器（实例 " + idx.ToString() + "）…", true);
                else
                    MessageBox.Show(this, "启动 dnplayer.exe 失败，请手动启动模拟器。", APP, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (dirty)
            {
                DialogResult r = MessageBox.Show(this, "有未保存的修改，确定要退出吗？", APP, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) e.Cancel = true;
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
            else if (e.Control && e.KeyCode == Keys.S) Save();
        }

        // ------------------------------------------------------------
        // 自动探测安装目录
        // ------------------------------------------------------------
        static string DetectInstallDir()
        {
            // 1) 正在运行的模拟器
            try
            {
                Process[] ps = Process.GetProcessesByName("dnplayer");
                foreach (Process p in ps)
                {
                    try
                    {
                        string f = p.MainModule.FileName;
                        if (!string.IsNullOrEmpty(f) && File.Exists(f)) return Path.GetDirectoryName(f);
                    }
                    catch { }
                }
            }
            catch { }

            // 2) 注册表
            string[] keys = new string[]
            {
                @"SOFTWARE\leidian\ldplayer9",
                @"SOFTWARE\WOW6432Node\leidian\ldplayer9",
                @"SOFTWARE\leidian\ldplayer",
                @"SOFTWARE\WOW6432Node\leidian\ldplayer",
                @"SOFTWARE\ChangZhi2\ldplayer9",
                @"SOFTWARE\WOW6432Node\ChangZhi2\ldplayer9"
            };
            string[] names = new string[] { "InstallDir", "installDir", "Path", "path", "", "InstallPath" };
            foreach (string kn in keys)
            {
                foreach (RegistryKey root in new RegistryKey[] { Registry.LocalMachine, Registry.CurrentUser })
                {
                    try
                    {
                        using (RegistryKey k = root.OpenSubKey(kn))
                        {
                            if (k == null) continue;
                            foreach (string vn in names)
                            {
                                object v = k.GetValue(vn);
                                if (v == null) continue;
                                string d = NormalizeDir(v.ToString());
                                if (d.Length > 0 && File.Exists(Path.Combine(d, "dnplayer.exe"))) return d;
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3) 常见路径
            string[] folderNames = new string[]
            {
                "LDPlayer14", "LDPlayer9", "LDPlayer4", "LDPlayer",
                @"LDPlayer\LDPlayer9", @"LDPlayer\LDPlayer14", "leidian", @"leidian\LDPlayer9"
            };
            try
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.DriveType != DriveType.Fixed) continue;
                    foreach (string fn in folderNames)
                    {
                        try
                        {
                            string d = Path.Combine(di.RootDirectory.FullName, fn);
                            if (File.Exists(Path.Combine(d, "dnplayer.exe"))) return d;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return "";
        }
    }

    // ==================================================================
    // 入口
    // ==================================================================
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length >= 3 && args[0] == "--test") { RoundTrip(args[1], args[2], args.Length > 3 ? args[3] : "add"); return; }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate (object s, ThreadExceptionEventArgs e) { ShowError(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e) { ShowError(e.ExceptionObject as Exception); };

            MainForm f = new MainForm();

            string shotPath = null;
            string dirArg = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--dir" && i + 1 < args.Length) dirArg = args[++i];
                else if (args[i] == "--shot" && i + 1 < args.Length) shotPath = args[++i];
            }
            if (dirArg != null) f.ForceDir = dirArg;
            if (shotPath != null)
            {
                Shooter(f, shotPath);
                return;
            }

            Application.Run(f);
        }

        // 隐藏自检：把 A 文件读入、添加一个映射后另存为 B，用于校验 XML 读写是否正确
        static void RoundTrip(string src, string dst)
        {
            RoundTrip(src, dst, "add");
        }

        static void RoundTrip(string src, string dst, string mode)
        {
            VBoxModel m = VBoxModel.Load(src);
            if (mode == "del")
            {
                if (m.Ports.Count > 0) m.Ports.RemoveAt(m.Ports.Count - 1);
            }
            else
            {
                UartPort p = new UartPort();
                p.Slot = 1; p.Enabled = true; p.IOBase = "0x2f8"; p.IRQ = "3"; p.Path = "COM3"; p.HostMode = "HostDevice";
                m.Ports.Add(p);
            }
            m.FilePath = dst;
            m.Save();
        }

        static void Shooter(MainForm f, string path)        {
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(-4000, -4000);
            f.Show();
            for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(60); }
            using (Bitmap bmp = new Bitmap(f.Width, f.Height))
            {
                f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                bmp.Save(path, ImageFormat.Png);
            }
            f.Close();
        }

        static void ShowError(Exception ex)
        {
            try
            {
                MessageBox.Show("程序遇到错误：\r\n\r\n" + (ex == null ? "未知错误" : ex.ToString()),
                    "模拟器串口配置", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
