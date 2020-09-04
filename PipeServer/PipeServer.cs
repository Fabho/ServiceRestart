using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PipeServer
{
    public class PipeServer
    {
        Process pipeClient { get; set; }
        private TaskCompletionSource<bool> eventHandled { get; set; }
        void StartProcess()
        {
            this.pipeClient = new Process();
            pipeClient.StartInfo.FileName = "pipeClient.exe";
            this.eventHandled = new TaskCompletionSource<bool>();


            using (AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
            {
                Console.WriteLine("[SERVER] Current TransmissionMode: {0}.", pipeServer.TransmissionMode);

                // Pass the client process a handle to the server.
                pipeClient.StartInfo.Arguments = pipeServer.GetClientHandleAsString();
                pipeClient.StartInfo.UseShellExecute = false;
                // para el exited
                pipeClient.EnableRaisingEvents = true;
                pipeClient.Exited += new EventHandler(myProcess_Exited);
                pipeClient.Start();

                pipeServer.DisposeLocalCopyOfClientHandle();

                try
                {
                    // Read user input and send that to the client process.
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        sw.AutoFlush = true;
                        // Send a 'sync message' and wait for client to receive it.
                        sw.WriteLine("SYNC");
                        pipeServer.WaitForPipeDrain();
                        // Send the console input to the client process.
                        Console.Write("[SERVER] Enter text: ");
                        sw.WriteLine(Console.ReadLine());
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e)
                {
                    Console.WriteLine("[SERVER] Error: {0}", e.Message);
                }
            }

            pipeClient.WaitForExit();
            pipeClient.Close();
            Console.WriteLine("[SERVER] Client quit. Server terminating.");
            Console.ReadLine();
        }

        static void Main()
        {
            PipeServer pipeServer = new PipeServer();
            pipeServer.StartProcess();
        }

        // Handle Exited event and display process information.
        private void myProcess_Exited(object sender, System.EventArgs e)
        {
            Console.WriteLine(
                $"Exit time    : {pipeClient.ExitTime}\n" +
                $"Exit code    : {pipeClient.ExitCode}\n" +
                $"Elapsed time : {Math.Round((pipeClient.ExitTime - pipeClient.StartTime).TotalMilliseconds)}");
            eventHandled.TrySetResult(true);

            Console.WriteLine("Deberia abrir otro window");
            PipeServer pipeServer = new PipeServer();
            pipeServer.StartProcess();
        }

    }
}
