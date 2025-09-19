using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


        private void AutomationWorker_DoWork()
        {
            EGUConverter eguConverter = new EGUConverter();
            DateTime lastUpdate = DateTime.Now;

            PointIdentifier K = new PointIdentifier(PointType.ANALOG_OUTPUT, 2000);
            PointIdentifier T1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1000);
            PointIdentifier T2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1001);
            PointIdentifier T3 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1002);
            PointIdentifier T4 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 1003);
            PointIdentifier I1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000);
            PointIdentifier I2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001);

            List<PointIdentifier> allPoints = new List<PointIdentifier> { K, T1, T2, T3, T4, I1, I2 };

            while (!disposedValue)
            {
                List<IPoint> points = storage.GetPoints(allPoints);

                IAnalogPoint kPoint = points[0] as IAnalogPoint;
                IDigitalPoint t1 = points[1] as IDigitalPoint;
                IDigitalPoint t2 = points[2] as IDigitalPoint;
                IDigitalPoint t3 = points[3] as IDigitalPoint;
                IDigitalPoint t4 = points[4] as IDigitalPoint;
                IDigitalPoint i1 = points[5] as IDigitalPoint;
                IDigitalPoint i2 = points[6] as IDigitalPoint;

                double K_value = kPoint.EguValue;
                double lowLimit = kPoint.ConfigItem.LowLimit;
                double eguMax = kPoint.ConfigItem.EGU_Max;

                if ((DateTime.Now - lastUpdate).TotalSeconds >= 1)
                {
                    lastUpdate = DateTime.Now;

                    if (t1.RawValue == 1) K_value -= 1;
                    if (t2.RawValue == 1) K_value -= 1;
                    if (t3.RawValue == 1) K_value -= 1;
                    if (t4.RawValue == 1) K_value -= 3;
                    if (i1.RawValue == 1) K_value += 2;
                    if (i2.RawValue == 1) K_value += 3;

                    if (K_value < 0) K_value = 0;
                    if (K_value > 100) K_value = 100;

                    processingManager.ExecuteWriteCommand(
                        kPoint.ConfigItem,
                        configuration.GetTransactionId(),
                        configuration.UnitAddress,
                        kPoint.ConfigItem.StartAddress,
                        (int)K_value
                    );
                }

                //ovo pise u napomeni napocetku
                if (i1.RawValue == 1 && i2.RawValue == 1)
                {
                    processingManager.ExecuteWriteCommand(i1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, i1.ConfigItem.StartAddress, 0);
                }

                if (K_value < lowLimit)
                {
                    //task1
                    if (t4.RawValue == 1)
                    {
                        processingManager.ExecuteWriteCommand(t4.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, t4.ConfigItem.StartAddress, 0);
                    }
                    if (i2.RawValue != 1)
                    {
                        processingManager.ExecuteWriteCommand(i2.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, i2.ConfigItem.StartAddress, 1);

                        if (i1.RawValue == 1)
                        {
                            processingManager.ExecuteWriteCommand(i1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, i1.ConfigItem.StartAddress, 0);
                        }

                    }
                }

                //task2
                if (K_value >= eguMax)
                {
                    if (i1.RawValue == 1)
                    {
                        processingManager.ExecuteWriteCommand(i1.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, i1.ConfigItem.StartAddress, 0);
                    }
                    if (i2.RawValue == 1)
                    {
                        processingManager.ExecuteWriteCommand(i2.ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, i2.ConfigItem.StartAddress, 0);
                    }
                }
                automationTrigger.WaitOne(delayBetweenCommands);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
