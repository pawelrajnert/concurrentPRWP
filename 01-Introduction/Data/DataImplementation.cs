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
            double xMin = 0;
            double xMax = 400 - ballDiameter - boxBorder * 2 + 1;

            double yMin = 0;
            double yMax = 420 - ballDiameter - boxBorder * 2 + 1;

            while (!Disposed)
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
                    lock(zamek) {
                // Sprawdzenie kolizji z innymi piłkami
                foreach (Ball otherBall in BallsList)
                    {
                        if (otherBall == ball)
                            continue;

                        Vector otherPosition = otherBall.getPosition();
                        double dx = newPosition.x - otherPosition.x;
                        double dy = newPosition.y - otherPosition.y;
                        double distanceSquared = dx * dx + dy * dy;
                        double ballDiameterSquared = ballDiameter * ballDiameter;

                        if (distanceSquared < ballDiameterSquared && distanceSquared > 0)
                        {
                            double distance = Math.Sqrt(distanceSquared);
                            Vector direction = new Vector(dx / distance, dy / distance);

                            Vector relativeVelocity = new Vector(
                                ball.Velocity.x - otherBall.Velocity.x,
                                ball.Velocity.y - otherBall.Velocity.y
                            );

                            double velocityAlongNormal = relativeVelocity.x * direction.x + relativeVelocity.y * direction.y;

                            if (velocityAlongNormal < 0) // Kolizja tylko jeśli piłki się zbliżają
                            {
                                // Oblicz nowe prędkości po zderzeniu
                                double m1 = ball.Mass;
                                double m2 = otherBall.Mass;

                                double impulse = (2 * velocityAlongNormal) / (m1 + m2);
                                Vector impulseVector = new Vector(
                                    impulse * direction.x,
                                    impulse * direction.y
                                );

                                ball.Velocity = new Vector(
                                    ball.Velocity.x - impulseVector.x * m2,
                                    ball.Velocity.y - impulseVector.y * m2
                                );

                                otherBall.Velocity = new Vector(
                                    otherBall.Velocity.x + impulseVector.x * m1,
                                    otherBall.Velocity.y + impulseVector.y * m1
                                );

                                // Przesunięcie piłek, aby zapobiec ich "przenikaniu"
                                double overlap = ballDiameter - distance;
                                Vector correction = new Vector(
                                    (overlap / 2) * direction.x,
                                    (overlap / 2) * direction.y
                                );

                                ball.Move(new Vector(-correction.x, -correction.y));
                                otherBall.Move(new Vector(correction.x, correction.y));
                            }
                        }
                    }

                    // Aktualizacja pozycji piłki
                    ball.Move(new Vector(ball.Velocity.x, ball.Velocity.y));
                }
                Thread.Sleep(10); // Odświeżanie co 10 ms
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