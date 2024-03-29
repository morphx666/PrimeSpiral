﻿using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using System.Threading.Tasks;
using System.Threading;

namespace PrimeSpiral {
    public class MainForm : Form {
        protected Drawable Canvas;

        private struct Spiral {
            public enum Directions {
                Right,
                Up,
                Left,
                Down,
            }

            public float X { get; private set; }
            private int Xc;
            private int Xn;
            public float Y { get; private set; }
            private int Yc;
            private int Yn;
            private Directions Direction;
            public float Step { get; }

            public Spiral(float step, float x, int xn, float y, int yn, Directions d) {
                X = x;
                Xc = 0;
                Xn = xn;
                Y = y;
                Yc = 0;
                Yn = yn;
                Direction = d;
                Step = step;
            }

            public void Next() {
                switch(Direction) {
                    case Directions.Left:
                    case Directions.Right:
                        if(Xc == Xn) {
                            Xc = 0;
                            Xn++;
                            NextDirection();
                            Next();
                        } else {
                            Xc++;
                            X += Step * ((Direction == Directions.Right) ? 1 : -1);
                        }
                        break;
                    case Directions.Up:
                    case Directions.Down:
                        if(Yc == Yn) {
                            Yc = 0;
                            Yn++;
                            NextDirection();
                            Next();
                        } else {
                            Yc++;
                            Y += Step * ((Direction == Directions.Down) ? 1 : -1);
                        }
                        break;
                }
            }

            void NextDirection() {
                int d = (int)Direction;
                d = (d + 1) % 4;
                Direction = (Directions)d;
            }

            public PointF Location {
                get => new PointF(X, Y);
            }
        }

        Spiral spiral;
        readonly List<PointF> points = new List<PointF>();
        UInt64 index = 1;
        List<(ulong Prime, RectangleF Bounds)> primes = new List<(ulong, RectangleF)>();
        float step = 20;
        float step2;
        float step4;

        (ulong Prime, RectangleF Bounds)? primeInfo = null;
        readonly Font font = new Font(FontFamilies.Monospace, 12);

        public MainForm() {
            JsonReader.Load(this);

            this.SizeChanged += (sender, e) => {
                Canvas.Width = this.Width;
                Canvas.Height = this.Height;

                lock(points) {
                    points.Clear();
                    primes.Clear();
                    index = 1;

                    spiral = new Spiral(step,
                        Canvas.Width / 2, 1,
                        Canvas.Height / 2, 1,
                        Spiral.Directions.Right);

                    AddPoint();
                }
            };

            this.MouseMove += HandleMouseMove;

            Canvas.Paint += (object s, PaintEventArgs e) => DrawSpiral(e);

            this.Maximize();

            this.Shown += (_, __) => {
                step2 = step / 2;
                step4 = step2 / 2;

                Task.Run(() => {
                    while(true) {
                        if(!((spiral.X < 0 || spiral.X > Canvas.Width) &&
                             (spiral.Y < 0 || spiral.Y > Canvas.Height))) {
                            AddPoint();
                        }
                    }
                });

                Task.Run(() => {
                    while(true) {
                        Thread.Sleep(30);
                        Canvas.Invalidate();
                    }
                });
            };
        }

        private void HandleMouseMove(object o, MouseEventArgs e) {
            PointF mp = e.Location;
            mp.Offset(-step2, -step2);

            lock(points) {
                foreach((ulong Prime, RectangleF Bounds) p in primes) {
                    if(IsInsideCircle(mp.X, mp.Y, p.Bounds)) {
                        primeInfo = p;
                        return;
                    }
                }
            }
            primeInfo = null;
        }

        void DrawSpiral(PaintEventArgs e) {
            Graphics g = e.Graphics;

            lock(points) {
                g.DrawLines(Colors.DimGray, points);

                foreach((ulong, RectangleF Bounds) p in primes) {
                    g.FillEllipse(Colors.White, p.Bounds);
                }
            }

            if(primeInfo.HasValue) {
                string info = $"{primeInfo.Value.Prime}";
                SizeF s = g.MeasureString(font, info);

                PointF p = primeInfo.Value.Bounds.Location;
                p.Offset(step2 + 20, step4 - s.Height / 2);

                RectangleF b = new RectangleF(p, s);
                b.Inflate(s.Height / 2, s.Height / 2);

                g.DrawEllipse(Colors.Red, primeInfo.Value.Bounds);
                g.FillRectangle(Colors.Black, b);
                g.DrawRectangle(Colors.Gray, b);
                g.DrawText(font, Brushes.White, p, info);
            }
        }

        void AddPoint() {
            lock(points) {
                if(IsPrime(index)) primes.Add((index, new RectangleF(spiral.Location.X - step4, spiral.Location.Y - step4, step2, step2)));
                index++;
                points.Add(spiral.Location);
                spiral.Next();
            }
        }

        bool IsPrime(UInt64 n) {
            UInt64 k = n;
            while(--k > 1) if(n % k == 0) return false;
            return n > 1;
        }

        bool IsInsideCircle(float x, float y, RectangleF r) {
            x -= r.X + r.Width / 2;
            y -= r.Y + r.Height / 2;
            float rd = r.Width / 2;
            float rd2 = rd * rd;
            return (x * x) / rd2 + (y * y) / rd2 <= 1.0;
        }
    }
}