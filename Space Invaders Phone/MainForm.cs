using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Space_Invaders_Phone.Properties;

namespace Space_Invaders_Phone
{
    public sealed partial class MainForm : Form
    {
        public static GameScreen GameScreen;

        public MainForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            GameScreen = new GameScreen();
            GameScreen.InitScreen(this);
        }
    }

    public class GameScreen
    {
        public static Bitmap Heart;
        public static List<Bitmap> Numbers;
        private Barricade[] _barricades;
        private const int SpeedUpFrequency = 5;
        private const int UfoFrequency = 5000;
        public int Shooters => (from l in _aliveEnemies from t in l where t != null select t.IsBottom ? 1 : 0).Sum();
        private const int EnemyStartSpeed = 60;
        private int _levelsPassed;
        private Timer _keyPressedTimer;
        private Keys _k;
        private int _movestate;
        private const int ColumnSize = 48;
        private int _columns;
        private Timer _updateTimer;
        public int Lives;
        public int Score;
        private List<List<Enemy>> _aliveEnemies;
        private List<List<Enemy>> _deadEnemies;
        public Player CurrentPlayer;
        private Ufo _visibleUfo;
        private Form _owner = Form.ActiveForm;
        public Rectangle Bounds;

        public void InitScreen(Form owner)
        {
            _movestate = 0;
            _levelsPassed = 0;
            Lives = 3;
            Score = 0;
            _keyPressedTimer = new Timer();
            _updateTimer = new Timer();
            _owner = owner;
            Bounds = new Rectangle(6, 39, _owner.ClientRectangle.Width - 12, _owner.ClientRectangle.Height - 51);
            Numbers = new List<Bitmap>
            {
                Resources._0,
                Resources._1,
                Resources._2,
                Resources._3,
                Resources._4,
                Resources._5,
                Resources._6,
                Resources._7,
                Resources._8,
                Resources._9
            };
            Heart = Resources.heart;
            _columns = Bounds.Width / ColumnSize - 1;
            CurrentPlayer = new Player(Bounds.Width / 2, Bounds.Height - 48);
            var reference = new Barricade(0);
            int barricadeWidth = reference.Container.Width;
            int barricadeNum = (Bounds.Width - 73) / (barricadeWidth + 73);
            _barricades = new Barricade[barricadeNum];
            if (barricadeNum > 0)
            {
                int barricadeStart = (Bounds.Width - barricadeNum * barricadeWidth - (barricadeNum - 1) * 85) / 2;
                _barricades[0] = new Barricade(barricadeStart);
                for (int i = 1; i < _barricades.Length; i++)
                    _barricades[i] = new Barricade(_barricades[i - 1].X + barricadeWidth + 85);
            }
            _updateTimer.Interval = 10;
            _updateTimer.Enabled = true;
            _updateTimer.Tick += Update;
            owner.Paint += Draw;
            owner.KeyDown += KeyPressed;
            owner.KeyUp += KeyUp;
            _keyPressedTimer.Interval = 10;
            _keyPressedTimer.Tick += Move;
            AllEnemiesKilled();
        }

        public void InitScreen()
        {
            _keyPressedTimer.Enabled = false;
            _levelsPassed = 0;
            Lives = 3;
            Score = 0;
            CurrentPlayer = new Player(Bounds.Width / 2, Bounds.Height - 48);
            AllEnemiesKilled();
            _updateTimer.Enabled = true;
            _barricades = new Barricade[4];
            _barricades[0] = new Barricade(85);
            for (int i = 1; i < _barricades.Length; i++)
                _barricades[i] = new Barricade(_barricades[i - 1].X + _barricades[i - 1].Container.Width + 85);
        }

        private void Move(object sender, EventArgs e)
        {
            CurrentPlayer.Move(_k);
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            _keyPressedTimer.Enabled = false;
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            _keyPressedTimer.Enabled = true;
            _k = e.KeyData;
        }

        public void Restart(Graphics g)
        {
            CurrentPlayer.Explode(g);
            Enemy.MovingLasers = new List<Laser>();
            _updateTimer.Interval = 11;
        }

        public void AllEnemiesKilled()
        {
            Enemy.RowsMoved = _levelsPassed;
            Enemy.Speed = EnemyStartSpeed - 2 * _levelsPassed;
            _aliveEnemies = new List<List<Enemy>>();
            _deadEnemies = new List<List<Enemy>>();
            Enemy.MovingLasers = new List<Laser>();
            for (int i = 0; i < 5; i++)
            {
                _aliveEnemies.Add(new List<Enemy>());
                _deadEnemies.Add(new List<Enemy>());
                for (int j = 0; j < _columns; j++)
                    _deadEnemies[i].Add(null);
            }
            for (int i = 0; i < _columns; i++)
                _aliveEnemies[0]
                    .Add(new Enemy(51 + i * ColumnSize, 0, 40, false, Resources.Costume_0_40_Points,
                        Resources.Costume_1_40_Points));
            for (int j = 0; j < 2; j++)
            for (int i = 0; i < _columns; i++)
                _aliveEnemies[1 + j]
                    .Add(new Enemy(48 + i * ColumnSize, 1 + j, 20, false, Resources.Costume_0_20_Points,
                        Resources.Costume_1_20_Points));
            for (int i = 0; i < _columns; i++)
                _aliveEnemies[3]
                    .Add(new Enemy(48 + i * ColumnSize, 3, 10, false, Resources.Costume_0_10_Points,
                        Resources.Costume_1_10_Points));
            for (int i = 0; i < _columns; i++)
                _aliveEnemies[4]
                    .Add(new Enemy(48 + i * ColumnSize, 4, 10, true, Resources.Costume_0_10_Points,
                        Resources.Costume_1_10_Points));
            _levelsPassed++;
        }

        private void Update(object sender, EventArgs e)
        {
            if (_updateTimer.Interval == 1000)
            {
                _updateTimer.Interval = 10;
                CurrentPlayer = new Player(CurrentPlayer.X, CurrentPlayer.Y);
            }
            if (_updateTimer.Interval == 11)
                _updateTimer.Interval = 1000;
            if (Lives == 0)
                GameOver();
            bool done = false;
            foreach (var l in _aliveEnemies)
            {
                if (done)
                    break;
                foreach (var t in l)
                {
                    if (t == null)
                        continue;
                    done = t.UpdateDirection();
                    if (done)
                        break;
                }
            }
            bool enemiesKilled = true;
            for (int i = 0; i < _aliveEnemies.Count; i++)
            for (int j = 0; j < _aliveEnemies[i].Count; j++)
            {
                if (_aliveEnemies[i][j] == null)
                    continue;
                if (_aliveEnemies[i][j].Container.Y + _aliveEnemies[i][j].Container.Height >= CurrentPlayer.Y)
                    GameOver();
                enemiesKilled = false;
                if (_aliveEnemies[i][j].IsAlive) continue;
                _deadEnemies[i][j] = _aliveEnemies[i][j];
                PassAbility(i, j, i - 1, j);
                _aliveEnemies[i][j] = null;
                j--;
            }
            if (enemiesKilled)
            {
                Lives++;
                AllEnemiesKilled();
            }
            foreach (var t in _barricades)
            {
                foreach (var i in Enemy.MovingLasers)
                    t.Erode(i);
                t.Erode(Player.P);
            }
            foreach (var t in _barricades)
            foreach (var i in _aliveEnemies)
            foreach (var j in i)
                t.Erode(j);
            if (_visibleUfo != null)
            {
                if (_visibleUfo.Enabled)
                    _visibleUfo.Move();
                if (Enemy.R.Next(0, 999999) % UfoFrequency == 0 && !_visibleUfo.Enabled)
                    _visibleUfo =
                        new Ufo(Enemy.R.Next(0, 99999) % 2 == 0
                                ? Ufo.Direction.Left
                                : Ufo.Direction.Right)
                            {Enabled = true};
            }
            else
            {
                if (Enemy.R.Next(0, 999999) % UfoFrequency == 0)
                    _visibleUfo =
                        new Ufo(Enemy.R.Next(0, 99999) % 2 == 0
                                ? Ufo.Direction.Left
                                : Ufo.Direction.Right)
                            {Enabled = true};
            }
            if (Form.ActiveForm != null) Form.ActiveForm.Invalidate();
        }

        private void PassAbility(int i1, int j1, int i2, int j2)
        {
            if (_aliveEnemies[i1][j1].IsBottom)
                try
                {
                    _aliveEnemies[i2][j2].IsBottom = true;
                }
                catch (Exception ex)
                {
                    if (ex is NullReferenceException || ex is ArgumentNullException)
                        PassAbility(i1, j1, i2 - 1, j2);
                }
        }

        public static void DrawNumbers(Graphics e, Point location, int number)
        {
            string num = number.ToString();
            for (int i = 0; i < num.Length; i++)
            {
                e.DrawImage(Numbers[int.Parse(num.Substring(i, 1))], location);
                location.X += Numbers[i].Width + 3;
            }
        }

        public void Draw(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, Bounds.Width + 12, Bounds.Height + 51));
            DrawNumbers(e.Graphics, new Point(9, 9), Score);
            if (Lives < 10)
            {
                e.Graphics.DrawImage(Heart, Bounds.X + Bounds.Width - 6 - Numbers[0].Width - Heart.Width, 9);
                DrawNumbers(e.Graphics, new Point(Bounds.X + Bounds.Width - 3 - Numbers[0].Width, 9), Lives);
            }
            else
            {
                e.Graphics.DrawImage(Heart, Bounds.X + Bounds.Width - 9 - Numbers[0].Width * 2 - Heart.Width, 9);
                DrawNumbers(e.Graphics, new Point(Bounds.X + Bounds.Width - 6 - Numbers[0].Width * 2, 9), Lives);
            }
            bool moved = false;
            foreach (var t in _aliveEnemies)
            foreach (var j in t)
            {
                if (j == null)
                    continue;
                if (Player.P.IsTouching(j.Container))
                {
                    Player.P.Enabled = false;
                    j.Explode(e.Graphics);
                }
                else
                {
                    if (j.ExplodedTicks > 0)
                    {
                        j.Explode(e.Graphics);
                    }
                    else
                    {
                        if (_movestate == 0)
                        {
                            j.Move();
                            moved = true;
                        }
                        j.Draw(e.Graphics);
                    }
                }
            }
            if (moved)
            {
                Enemy.ColumnsMoved++;
                if (Enemy.ColumnsMoved % SpeedUpFrequency == 0 && Enemy.Speed > 1)
                    Enemy.Speed--;
            }
            for (int i = 0; i < Enemy.MovingLasers.Count; i++)
                if (!Enemy.MovingLasers[i].Enabled)
                {
                    Enemy.MovingLasers.RemoveAt(i);
                    i--;
                }
                else
                {
                    Enemy.MovingLasers[i].Draw(e.Graphics);
                    if (Enemy.MovingLasers[i].IsTouching(CurrentPlayer.Container))
                    {
                        Enemy.MovingLasers[i].Enabled = false;
                        Restart(e.Graphics);
                    }
                }
            foreach (var t in _barricades)
                if (t.Enabled)
                    t.Draw(e.Graphics);
            CurrentPlayer.Draw(e.Graphics);
            Player.P.Draw(e.Graphics);
            if (_visibleUfo != null)
                if (_visibleUfo.Enabled)
                    if (_visibleUfo.ExplosionFramesGone == 10)
                    {
                        Score += _visibleUfo.Points;
                        _visibleUfo.Enabled = false;
                    }
                    else
                    {
                        _visibleUfo.Draw(e.Graphics);
                        if (Player.P.IsTouching(_visibleUfo.Container))
                            _visibleUfo.Explode(e.Graphics);
                    }
            _movestate++;
            _movestate %= (int) Enemy.Speed + 1;
            e.Graphics.DrawRectangle(Pens.White, Bounds);
            e.Graphics.FillRectangle(Brushes.Black, 0, Bounds.Y, 6, Bounds.Height);
            e.Graphics.FillRectangle(Brushes.Black, Bounds.Width + Bounds.X + 1, Bounds.Y, 6, Bounds.Height);
        }

        private void GameOver()
        {
            _updateTimer.Enabled = false;
            _keyPressedTimer.Enabled = false;
        }
    }

    public class Player
    {
        public int MovementSpeed = 6;
        public static Laser P;
        public Image PlayerImage = Resources.Player;
        public Rectangle Container => new Rectangle(X, Y, PlayerImage.Width, PlayerImage.Height);
        public int X;
        public int Y;

        public Player(int x, int y)
        {
            X = x;
            Y = y;
            P = new Laser(x + Container.Width / 2, y, -9);
        }

        public void Shoot()
        {
            if (P.Enabled) return;
            P = new Laser(X + Container.Width / 2, Y, -9);
            P.Start();
        }

        public void Move(Keys k)
        {
            if (k == Keys.Right)
                if (X >= MainForm.GameScreen.Bounds.X + MainForm.GameScreen.Bounds.Width - Container.Width -
                    MovementSpeed)
                    X = MainForm.GameScreen.Bounds.X + MainForm.GameScreen.Bounds.Width - Container.Width -
                        MovementSpeed;
                else
                    X += MovementSpeed;
            if (k == Keys.Left)
                if (X <= MainForm.GameScreen.Bounds.X + MovementSpeed)
                    X = MainForm.GameScreen.Bounds.X + MovementSpeed;
                else
                    X -= MovementSpeed;
            if (k == Keys.Space)
                Shoot();
        }

        public void Draw(Graphics g)
        {
            g.DrawImage(PlayerImage, X, Y);
        }

        public void Erase(Graphics g)
        {
            g.FillRectangle(Brushes.Black, Container);
        }

        public void Explode(Graphics g)
        {
            Erase(g);
            PlayerImage = Resources.PlayerExploded;
            Draw(g);
            MainForm.GameScreen.Lives--;
        }
    }

    public class Laser
    {
        public Rectangle Container => new Rectangle(_x, _y, 3, 9);
        private const int Speed = 100;
        private readonly int _x;
        private int _y;
        private readonly int _deltaY;
        private readonly Timer _ = new Timer();

        public bool Enabled
        {
            get { return _.Enabled; }
            set { _.Enabled = value; }
        }

        public Laser(int x, int y, int deltaY)
        {
            _x = x;
            _y = y;
            _deltaY = deltaY;
        }

        public void Start()
        {
            _.Enabled = true;
            _.Interval = 100 / Speed;
            _.Tick += Tick;
        }

        private void Tick(object sender, EventArgs e)
        {
            Update();
        }

        public void Update()
        {
            if (_y + _deltaY <= MainForm.GameScreen.Bounds.Y || _y + _deltaY - Container.Height >=
                MainForm.GameScreen.Bounds.Height + MainForm.GameScreen.Bounds.Y)
                _.Enabled = false;
            else
                _y += _deltaY;
        }

        public void Draw(Graphics g)
        {
            if (Enabled)
                g.FillRectangle(Brushes.White, Container);
        }

        public void Erase(Graphics g)
        {
            g.FillRectangle(Brushes.Black, Container);
        }

        public bool IsTouching(Rectangle r)
        {
            if (!Enabled)
                return false;
            if (_x + Container.Width < r.X || _x > r.X + r.Width)
                return false;
            if (_y + Container.Height < r.Y || _y > r.Y + r.Height)
                return false;
            return true;
        }
    }

    public class Enemy
    {
        public static int ShootingFrequency = 3;
        public static int ColumnsMoved;
        public static int YPadding = 48 + 27;
        public static double Speed;
        public bool IsAlive = true;
        public bool IsBottom;
        public static List<Laser> MovingLasers = new List<Laser>();
        public static Random R = new Random();
        public int Points;
        public int X;
        public int InitYRow;
        public int Costume;
        public static int RowsMoved;
        public static int RowHeight = 40;
        public static int DeltaX = 3;
        public static int ExplodingTicks = 10;
        public int ExplodedTicks;

        public Rectangle Container => new Rectangle(X, (InitYRow + RowsMoved) * RowHeight + YPadding,
            Costumes[Costume].Width, Costumes[Costume].Height);

        public List<Bitmap> Costumes = new List<Bitmap>();
        private readonly Bitmap _exploded = Resources.EnemyExploded;

        public Enemy(int x, int row, int points, bool isBottom = false, params Bitmap[] costumes)
        {
            Points = points;
            InitYRow = row;
            X = x;
            IsBottom = isBottom;
            foreach (var t in costumes)
                Costumes.Add(new Bitmap(t));
        }

        public void Move()
        {
            if (R.Next(0, MainForm.GameScreen.Shooters * ShootingFrequency + 5) == 0 && IsBottom)
            {
                MovingLasers.Add(new Laser(Container.X + Container.Width / 2, Container.Y + Container.Height, 9));
                MovingLasers[MovingLasers.Count - 1].Start();
            }
            X += DeltaX;
            Costume++;
            Costume %= 2;
        }

        public void Draw(Graphics g)
        {
            g.DrawImage(Costumes[Costume], Container.X, Container.Y);
        }

        public void Erase(Graphics g)
        {
            g.FillRectangle(Brushes.Black, Container);
        }

        public void Explode(Graphics g)
        {
            Erase(g);
            if (ExplodedTicks <= ExplodingTicks / 2)
                g.DrawImage(_exploded, Container.X, Container.Y);
            if (ExplodedTicks > ExplodingTicks / 2)
            {
                var explosionCenter = new Point(Container.X + _exploded.Width / 2, Container.Y + _exploded.Height / 2);
                int textWidth = Points.ToString().Length * (GameScreen.Numbers[0].Width + 3) - 3;
                int textHeight = GameScreen.Numbers[0].Height;
                GameScreen.DrawNumbers(g,
                    new Point(explosionCenter.X - textWidth / 2, explosionCenter.Y - textHeight / 2), Points);
            }
            if (ExplodedTicks == ExplodingTicks)
            {
                IsAlive = false;
                MainForm.GameScreen.Score += Points;
                return;
            }
            ExplodedTicks++;
        }

        public bool UpdateDirection()
        {
            switch (DeltaX)
            {
                case -3:
                    if (X + DeltaX <= MainForm.GameScreen.Bounds.X)
                    {
                        DeltaX *= -1;
                        RowsMoved++;
                        return true;
                    }
                    break;
                case 3:
                    if (X + DeltaX + Container.Width >= MainForm.GameScreen.Bounds.X + MainForm.GameScreen.Bounds.Width)
                    {
                        DeltaX *= -1;
                        RowsMoved++;
                        return true;
                    }
                    break;
            }
            return false;
        }
    }

    public class Ufo
    {
        public bool Enabled;
        public static int ExplosionFrames = 10;
        public int ExplosionFramesGone;
        public static Random R = new Random();

        public enum Direction
        {
            Right,
            Left
        }

        public Direction DirectionMoving;
        public static Image UfoImage = Resources.UFO;
        public static Image ExplodedImage = new Bitmap(UfoImage.Width, UfoImage.Height);
        public const int Y = 48;
        public int X;
        public int Points;
        public Rectangle Container => new Rectangle(X, Y, UfoImage.Width, UfoImage.Height);

        public Ufo(Direction directionMoving)
        {
            DirectionMoving = directionMoving;
            Points = R.Next(1, 7) * 50;
            X = directionMoving == Direction.Left
                ? MainForm.GameScreen.Bounds.Width + MainForm.GameScreen.Bounds.X - 1
                : MainForm.GameScreen.Bounds.X - Container.Width + 1;
            Enabled = false;
        }

        public void Draw(Graphics g)
        {
            if (ExplosionFramesGone != 0)
                Explode(g);
            else
                g.DrawImage(UfoImage, Container.Location);
        }

        public void Explode(Graphics g)
        {
            GameScreen.DrawNumbers(g, Container.Location, Points);
            ExplosionFramesGone++;
        }

        public void Move()
        {
            if (ExplosionFramesGone == 0)
                X += DirectionMoving == Direction.Right ? 3 : -3;
            Enabled = true;
            switch (DirectionMoving)
            {
                case Direction.Right:
                    if (X >= MainForm.GameScreen.Bounds.X + MainForm.GameScreen.Bounds.Width)
                        Enabled = false;
                    break;
                case Direction.Left:
                    if (X + Container.Width <= MainForm.GameScreen.Bounds.X)
                        Enabled = false;
                    break;
            }
        }
    }

    public class BarricadeSection
    {
        public int HitsLeft;
        public Bitmap CurrentImage;
        public static List<Bitmap> FullStates;
        public int X;
        public int Y;
        public Rectangle Container => new Rectangle(X, Y, CurrentImage.Width, CurrentImage.Height);

        public BarricadeSection(Image startingImage, int x, int y)
        {
            X = x;
            Y = y;
            CurrentImage = new Bitmap(startingImage);
            HitsLeft = 4;
        }

        public void Draw(Graphics g)
        {
            if (HitsLeft == 0)
                return;
            g.DrawImage(CurrentImage, Container.Location);
        }

        public void Erode()
        {
            HitsLeft--;
            if (HitsLeft == 0)
                return;
            var b = FullStates[FullStates.Count - HitsLeft];
            for (int i = 0; i < b.Height; i++)
            for (int j = 0; j < b.Width; j++)
                if (b.GetPixel(j, i) != Color.FromArgb(0, 255, 0))
                    CurrentImage.SetPixel(j, i, b.GetPixel(j, i));
        }
    }

    public class Barricade
    {
        public int X;
        public static int Y = MainForm.GameScreen.Bounds.Height + MainForm.GameScreen.Bounds.X - 51 - 96;
        public List<BarricadeSection> Blocks;

        public bool Enabled
        {
            get
            {
                bool isEnabled = false;
                foreach (var t in Blocks) if (t.HitsLeft != 0) isEnabled = true;
                return isEnabled;
            }
        }

        public Rectangle Container => new Rectangle(X, Y, (Blocks[0].Container.Width + 1) * 4,
            (Blocks[0].Container.Height + 1) * 3);

        public Barricade(int x)
        {
            X = x;
            Blocks = new List<BarricadeSection> { new BarricadeSection(Resources.TopLeft, x, Y) };
            Blocks.Add(new BarricadeSection(Resources.Full, Blocks[0].X + Blocks[0].Container.Width + 1, Y));
            Blocks.Add(new BarricadeSection(Resources.Full, Blocks[1].X + Blocks[1].Container.Width, Y));
            Blocks.Add(new BarricadeSection(Resources.TopRight, Blocks[2].X + Blocks[2].Container.Width + 1, Y));
            Blocks.Add(new BarricadeSection(Resources.Full, Blocks[0].Container.X, Blocks[0].Y + Blocks[0].Container.Height + 1));
            Blocks.Add(new BarricadeSection(Resources.BottomLeft, Blocks[1].Container.X, Blocks[1].Y + Blocks[1].Container.Height + 1));
            Blocks.Add(new BarricadeSection(Resources.BottomRight, Blocks[2].Container.X, Blocks[2].Y + Blocks[2].Container.Height + 1));
            Blocks.Add(new BarricadeSection(Resources.Full, Blocks[3].Container.X, Blocks[3].Y + Blocks[3].Container.Height + 1));
            Blocks.Add(new BarricadeSection(Resources.Full, Blocks[4].Container.X, Blocks[4].Y + Blocks[4].Container.Height + 1));
            Blocks.Add(new BarricadeSection(Resources.Full, Blocks[7].Container.X, Blocks[7].Y + Blocks[7].Container.Height + 1));
            BarricadeSection.FullStates = new List<Bitmap>
            {
                Resources.BarricadeHits_0,
                Resources.BarricadeHits_1,
                Resources.BarricadeHits_2,
                Resources.BarricadeHits_3
            };
        }

        public void Erode(Laser laserHit)
        {
            foreach (var t in Blocks)
            {
                if (t.HitsLeft == 0)
                    continue;
                if (!laserHit.IsTouching(t.Container) || !laserHit.Enabled) continue;
                t.Erode();
                laserHit.Enabled = false;
            }
        }

        public void Erode(Enemy laserHit)
        {
            foreach (var t in Blocks)
            {
                if (t.HitsLeft == 0)
                    continue;
                if (laserHit == null) continue;
                bool isTouching = laserHit.IsAlive;
                if (t.X + t.Container.Width < laserHit.X || t.X > laserHit.X + laserHit.Container.Width)
                    isTouching = false;
                if (t.Y + t.Container.Height < laserHit.Container.Y ||
                    t.Y > laserHit.Container.Y + laserHit.Container.Height)
                    isTouching = false;
                if (isTouching)
                    t.Erode();
            }
        }

        public void Draw(Graphics g)
        {
            foreach (var t in Blocks)
                t.Draw(g);
        }
    }
}