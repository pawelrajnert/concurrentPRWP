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
using System.Threading.Tasks;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        #region ctor

        public DataImplementation()
        {
            
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
            double ballDiameter = 20;
            double boxBorder = 4;

            double xMin = boxBorder;
            double xMax = 400 - ballDiameter - boxBorder;
            double yMin = boxBorder;
            double yMax = 420 - ballDiameter - boxBorder;


            for (int i = 0; i < numberOfBalls; i++)
            {
                Vector startingPosition;
                bool positionOK;
                Vector velocity = new Vector((random.NextDouble()/2 * 5), (random.NextDouble()/2 *5));
                int attempts = 0;
                do
                {
                    positionOK = true;
                    startingPosition = new Vector(random.Next((int)xMin, (int)xMax), random.Next((int)yMin, (int)yMax));
                    foreach (Ball existingBall in BallsList)
                    {
                        Vector otherPosition = existingBall.getPosition();
                        

                        double dx = otherPosition.x - startingPosition.x;
                        double dy = otherPosition.y - startingPosition.y;
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        if(distance < ballDiameter)
                        {
                            positionOK = false;
                            break;
                        }

                    }
                    attempts++;
                    if (attempts > 100)
                        throw new Exception("Nie można znalzeźć pozycji startowej");

                } while (!positionOK);
                
                Ball newBall = new(startingPosition, velocity, 10);
                upperLayerHandler(startingPosition, newBall);
                lock (zamek) { BallsList.Add(newBall);}
                Thread ballThread = new Thread(() => Move(newBall));
                ballThread.IsBackground = true;
                lock (zamek)
                {
                    BallThreads.Add(ballThread);
                }
                ballThread.Start();
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

        private bool Disposed = false;
        private Random RandomGenerator = new();
        private List<Ball> BallsList = [];
        //private int[] masses = new int[] { 5, 10, 15 };
        private int constMass = 10;
        private readonly object zamek = new object();
        private List<Thread> BallThreads = [];
        private void Move(Ball ball)
        {
            double ballDiameter = 20;
            double boxBorder = 4;

            // Granice obszaru odbicia
            double xMin = boxBorder;
            double xMax = 400 - ballDiameter - boxBorder;

            double yMin = boxBorder;
            double yMax = 420 - ballDiameter - boxBorder;

            while (!Disposed)
            {
                lock (zamek)
                {
                    Vector position = ball.getPosition();
                    IVector velocity = ball.Velocity;

                    double xNew = position.x + velocity.x;
                    double yNew = position.y + velocity.y;

                    // Sprawdzenie odbicia od ścianek
                    if (xNew <= xMin || xNew >= xMax)
                    {
                        ball.Velocity = new Vector(-ball.Velocity.x, ball.Velocity.y); // Odbicie w osi X
                    }
                    if (yNew <= yMin || yNew >= yMax)
                    {
                        ball.Velocity = new Vector(ball.Velocity.x, -ball.Velocity.y); // Odbicie w osi Y
                    }

                    xNew = Math.Max(xMin, Math.Min(xNew, xMax));
                    yNew = Math.Max(yMin, Math.Min(yNew, yMax));
                    Vector newPosition = new Vector(xNew, yNew);

                    // Sprawdzenie kolizji z innymi piłkami
                    foreach (Ball otherBall in BallsList)
                    {
                        if (otherBall == ball)
                            continue;

                        Vector otherPosition = otherBall.getPosition();
                        double dx = otherPosition.x - newPosition.x;
                        double dy = otherPosition.y - newPosition.y;
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        if (distance < ballDiameter)
                        {
                            // Oblicz nowe prędkości po zderzeniu
                            Vector v1 = (Vector)ball.Velocity;
                            Vector v2 = (Vector)otherBall.Velocity;

                            Vector p1 = ball.getPosition();
                            Vector p2 = otherBall.getPosition();

                            Vector deltaP = new Vector(p1.x - p2.x, p1.y - p2.y);
                            Vector deltaV = new Vector(v1.x - v2.x, v1.y - v2.y);

                            double distanceSquared = deltaP.x * deltaP.x + deltaP.y * deltaP.y;
                            double dotProduct = deltaV.x * deltaP.x + deltaV.y * deltaP.y;

                            if (distanceSquared > 0) // Uniknięcie dzielenia przez zero
                            {
                                Vector v1New = new Vector(
                                    v1.x - (dotProduct / distanceSquared) * deltaP.x,
                                    v1.y - (dotProduct / distanceSquared) * deltaP.y
                                );

                                Vector v2New = new Vector(
                                    v2.x - (dotProduct / distanceSquared) * -deltaP.x,
                                    v2.y - (dotProduct / distanceSquared) * -deltaP.y
                                );

                                ball.Velocity = v1New;
                                otherBall.Velocity = v2New;

                                // Przesunięcie piłek, aby zapobiec ich "przenikaniu"
                                double overlap = ballDiameter - distance;
                                Vector correction = new Vector(
                                    (overlap / 2) * (deltaP.x / Math.Sqrt(distanceSquared)),
                                    (overlap / 2) * (deltaP.y / Math.Sqrt(distanceSquared))
                                );

                                ball.Move(new Vector(-correction.x, -correction.y));
                                otherBall.Move(new Vector(correction.x, correction.y));
                            }
                        }
                    }

                    // Aktualizacja pozycji piłki
                    ball.Move(new Vector(ball.Velocity.x, ball.Velocity.y));
                }
                Thread.Sleep(33); // Odświeżanie co 33 ms
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