
SET COMPILER_DIR="C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn"
%COMPILER_DIR%\csc.exe -target:exe -out:GenerateResponses.exe GenerateResponses.cs BaseEngineGRM.cs MCATEngineGRM.cs MCATEngineGRM5.cs

