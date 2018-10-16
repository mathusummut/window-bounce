using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowBounce.Properties;

namespace WindowBounce {
	public class Window : Form {
		private SolidBrush mouseOverBrush = new SolidBrush(Color.FromArgb(75, 61, 215, 232)),
			mouseDownBrush = new SolidBrush(Color.FromArgb(75, 36, 16, 218));
		private Bitmap defaultBackground, mouseOver, mouseDown, currentBackground;
		private Stopwatch stopwatch = new Stopwatch();
		private PointF currentLocation, velocity, oldMouseLocation;
		private Point oldMousePosition;
		private Rectangle bounds;
		private double exponential, acceleration;
		private bool isMouseDown, isMouseOver, fadeIn, isClosing;
		private IContainer components;
		private OpenFileDialog openFileDialog;
		private Timer timer;

		public Window() {
			SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint, true);
			InitializeComponent();
			defaultBackground = new Bitmap(Resources.Background, ClientSize);
			mouseOver = new Bitmap(defaultBackground);
			using (Graphics g = Graphics.FromImage(mouseOver))
				g.FillRectangle(mouseOverBrush, new Rectangle(Point.Empty, mouseOver.Size));
			mouseDown = new Bitmap(defaultBackground);
			using (Graphics g = Graphics.FromImage(mouseDown))
				g.FillRectangle(mouseDownBrush, new Rectangle(Point.Empty, mouseDown.Size));
			currentBackground = defaultBackground;
			SystemEvents.DisplaySettingsChanged += new EventHandler(SystemEvents_DisplaySettingsChanged);
		}

		[STAThread]
		public static void Main() {
			Application.SetCompatibleTextRenderingDefault(false);
			Application.EnableVisualStyles();
			Application.Run(new Window());
		}

		protected override void OnKeyUp(KeyEventArgs e) {
			base.OnKeyUp(e);
			Keys keyData = e.KeyData;
			if (keyData == Keys.Escape) {
				Close();
				return;
			} else if (keyData != Keys.F6)
				return;
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				Image temp = defaultBackground;
				try {
					defaultBackground = new Bitmap(new Bitmap(openFileDialog.FileName), ClientSize);
				} catch {
					MessageBox.Show("Image type not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
				temp.Dispose();
				mouseOver.Dispose();
				mouseOver = new Bitmap(defaultBackground);
				using (Graphics g = Graphics.FromImage(mouseOver))
					g.FillRectangle(mouseOverBrush, new Rectangle(Point.Empty, mouseOver.Size));
				mouseDown.Dispose();
				mouseDown = new Bitmap(defaultBackground);
				using (Graphics g = Graphics.FromImage(mouseDown))
					g.FillRectangle(mouseDownBrush, new Rectangle(Point.Empty, mouseDown.Size));
				currentBackground = isMouseDown ? mouseDown : (isMouseOver ? mouseOver : defaultBackground);
				Invalidate(false);
			}
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			isMouseDown = true;
			oldMouseLocation = new PointF(currentLocation.X + e.X, currentLocation.Y + e.Y);
			velocity = new PointF();
			currentBackground = mouseDown;
			oldMousePosition = Cursor.Position;
			base.OnMouseDown(e);
			Invalidate(false);
		}

		protected override void OnMouseEnter(EventArgs e) {
			currentBackground = mouseOver;
			isMouseOver = true;
			base.OnMouseEnter(e);
			Invalidate(false);
		}

		protected override void OnMouseLeave(EventArgs e) {
			currentBackground = defaultBackground;
			isMouseOver = false;
			base.OnMouseLeave(e);
			Invalidate(false);
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			if (isMouseDown) {
				if (oldMousePosition == Cursor.Position)
					return;
				oldMousePosition = Cursor.Position;
				PointF vector = new PointF(currentLocation.X + e.X, currentLocation.Y + e.Y);
				velocity = new PointF(vector.X - oldMouseLocation.X, vector.Y - oldMouseLocation.Y);
				oldMouseLocation = vector;
				currentLocation.X += velocity.X;
				currentLocation.Y += velocity.Y;
				if (currentLocation.X < 0f)
					currentLocation.X = 0f;
				else if (Width + currentLocation.X > bounds.Width)
					currentLocation.X = bounds.Width - Width;
				if (currentLocation.Y < 0f)
					currentLocation.Y = 0f;
				else if (Height + currentLocation.Y > bounds.Height)
					currentLocation.Y = bounds.Height - Height;
				Location = new Point((int) currentLocation.X, (int) currentLocation.Y);
				stopwatch.Reset();
				stopwatch.Start();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			isMouseDown = false;
			currentBackground = mouseOver;
			if (stopwatch.ElapsedMilliseconds > 500L)
				velocity = new PointF();
			stopwatch.Stop();
			base.OnMouseUp(e);
			Invalidate(false);
		}

		protected override void OnPaintBackground(PaintEventArgs e) {
		}

		protected override void OnPaint(PaintEventArgs e) {
			e.Graphics.CompositingMode = CompositingMode.SourceCopy;
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.DrawImage(currentBackground, 0, 0, Width, Height);
		}

		protected override void OnShown(EventArgs e) {
			base.OnShown(e);
			bounds = Screen.GetBounds(this);
			currentLocation = new PointF((bounds.Width - Width) * 0.5f, (bounds.Height - Width) * 0.5f);
			Location = new Point((int) currentLocation.X, (int) currentLocation.Y);
			fadeIn = true;
			timer.Enabled = true;
		}

		private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) {
			bounds = Screen.GetBounds(this);
		}

		private void timer_Tick(object sender, EventArgs e) {
			if (fadeIn) {
				exponential += 0.007;
				acceleration = acceleration + 0.006 * exponential;
				Opacity = Opacity + acceleration;
				if (Opacity >= 0.9) {
					Opacity = 0.9;
					fadeIn = false;
				}
			} else if (isClosing) {
				Opacity -= Opacity * 0.2;
				if (Opacity <= 0.01 || Opacity == 1.0) {
					timer.Dispose();
					Close();
				}
			}
			if (!isMouseDown) {
				velocity.Y += 0.5f;
				velocity.X *= 0.992063463f;
				velocity.Y *= 0.992063463f;
				currentLocation.X += velocity.X;
				currentLocation.Y += velocity.Y;
				if (currentLocation.X < 0f) {
					velocity.X = -velocity.X;
					currentLocation.X = 0f;
				} else if (Width + currentLocation.X >= bounds.Width) {
					velocity.X = -velocity.X;
					currentLocation.X = bounds.Width - Width;
				}
				if (currentLocation.Y < 0f) {
					velocity.Y = -velocity.Y;
					currentLocation.Y = 0f;
				} else if (Height + currentLocation.Y >= bounds.Height) {
					velocity.Y = -velocity.Y;
					currentLocation.Y = bounds.Height - Height;
				}
				Location = new Point((int) currentLocation.X, (int) currentLocation.Y);
			}
		}

		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Window));
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			// 
			// timer
			// 
			this.timer.Interval = 1;
			this.timer.Tick += new System.EventHandler(this.timer_Tick);
			// 
			// openFileDialog
			// 
			this.openFileDialog.AddExtension = false;
			this.openFileDialog.FileName = "Image.jpg";
			this.openFileDialog.Filter = "Common Image Files|*.*";
			this.openFileDialog.Title = "Choose background image...";
			// 
			// Window
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(246, 200);
			this.Cursor = System.Windows.Forms.Cursors.Default;
			this.ForeColor = System.Drawing.Color.White;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Window";
			this.Opacity = 0D;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Window Bounce";
			this.TopMost = true;
			this.ResumeLayout(false);

		}

		protected override void OnClosing(CancelEventArgs e) {
			if (!isClosing) {
				isClosing = true;
				e.Cancel = true;
			}
			base.OnClosing(e);
		}

		protected override void Dispose(bool disposing) {
			components.Dispose();
			defaultBackground.Dispose();
			mouseOver.Dispose();
			mouseDown.Dispose();
			base.Dispose(disposing);
		}
	}
}