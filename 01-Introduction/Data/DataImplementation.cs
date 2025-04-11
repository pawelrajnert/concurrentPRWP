//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        #region ctor

        public DataImplementation()
        { // 33 ms to około 30 klatek na sekunde, jest to wartość wystarczająca dla obrazowania kul
            MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(33));
        }

        #endregion ctor

        #region DataAbstractAPI

        public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));
            Random random = new Random();
            for (int i = 0; i < numberOfBalls; i++)
            {
                Vector startingPosition = new(random.Next(100, 400 - 100), random.Next(100, 400 - 100));
                Ball newBall = new(startingPosition, startingPosition);
                upperLayerHandler(startingPosition, newBall);
                BallsList.Add(newBall);
            }
        }

        #endregion DataAbstractAPI

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    MoveTimer.Dispose();
                    BallsList.Clear();
                }
                Disposed = true;
            }
            else
                throw new ObjectDisposedException(nameof(DataImplementation));
        }

        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

        //private bool disposedValue;
        private bool Disposed = false;

        private readonly Timer MoveTimer;
        private Random RandomGenerator = new();
        private List<Ball> BallsList = [];

        private void Move(object? x)
        {
            // obecnie ustawione wartości w programie:
            // średnica kulki = 20, wymiary obszaru odbicia kul 420 x 400, 4 to grubość obramowania
            double ballDiameter = 20;
            double boxBorder = 4;

            double xMin = boxBorder;
            double xMax = 400 - ballDiameter - boxBorder;

            double yMin = boxBorder;
            double yMax = 420 - ballDiameter - boxBorder;


            foreach (Ball item in BallsList)
            {
                // kolejny ruch kulki pozostawiamy w taki sam sposób jak był- jest on losowy
                Vector randomNextMove = new Vector((RandomGenerator.NextDouble() - 0.5) * 10, (RandomGenerator.NextDouble() - 0.5) * 10);

                // odczytujemy obecną pozycję danej kulki
                Vector currentPosition = item.getPosition();

                // nowa pozycja kulki to pozycja obecna + wylosowane dodatkowe przesunięcie
                double xNew = currentPosition.x + randomNextMove.x;
                double yNew = currentPosition.y + randomNextMove.y;

                // sprawdzamy czy nowo utworzona pozycja nie wyjeżdża poza obrys obszaru
                xNew = Math.Max(xMin, Math.Min(xNew, xMax));
                yNew = Math.Max(yMin, Math.Min(yNew, yMax));

                item.Move(new Vector(xNew - currentPosition.x, yNew - currentPosition.y));
            }
        }

        #endregion private

        #region TestingInfrastructure

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
        {
            returnBallsList(BallsList);
        }

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
        {
            returnNumberOfBalls(BallsList.Count);
        }

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }

        #endregion TestingInfrastructure
    }
}