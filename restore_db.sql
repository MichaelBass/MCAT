/* change the path of the back-up and location */
RESTORE DATABASE Simulation_2019_05_24 FROM DISK = 'C:\code\mcat\Simulaation_2019_05_24.bak'
WITH MOVE 'Simulation_2019_05_24' TO 'C:\code\mcat\Simulation_2019_05_24.mdf',
MOVE 'Simulation_2019_05_24_log' TO 'C:\code\mcat\Simulation_2019_05_24_Log.ldf'
GO
