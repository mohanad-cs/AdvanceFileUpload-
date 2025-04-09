using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using MassTransit;
using AdvanceFileUpload.Integration.Contracts;
using static AdvanceFileUpload.Integration.Contracts.IntegrationConstants;
using System.IO;

namespace AdvanceFileUpload.Samples.WinIntegrationSample
{
    public partial class PDFViewer : DevExpress.XtraEditors.XtraForm
    {
        public PDFViewer()
        {
            InitializeComponent();

           // pdfViewer1.LoadDocument(@"D:\Temp\Savedir\3522 صورة بوليصة.pdf");
            this.Load += PDFViewer_Load;
        }

        private void PDFViewer_Load(object? sender, EventArgs e)
        {
            RabbitMQOptions rabbitMQOptions = new RabbitMQOptions()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                UseSSL = false,
            };

            var sessionCreatedConsumer = new RabbitMQConsumer(rabbitMQOptions);
            sessionCreatedConsumer.Consume<SessionCreatedIntegrationEvent>(new ConsumingArgs()
            {
                Exchange = IntegrationConstants.SessionCreatedConstants.Exchange,
                ExchangeType = IntegrationConstants.SessionCreatedConstants.ExchangeType,
                AutoDelete = IntegrationConstants.SessionCreatedConstants.AutoDelete,
                Durable = IntegrationConstants.SessionCreatedConstants.Durable,
                Exclusive = IntegrationConstants.SessionCreatedConstants.Exclusive,
                Queue = IntegrationConstants.SessionCreatedConstants.Queue,
                RoutingKey = IntegrationConstants.SessionCreatedConstants.RoutingKey,

            }, OnSessionCreated);

            var sessionPausedConsumer = new RabbitMQConsumer(rabbitMQOptions);
            sessionPausedConsumer.Consume<SessionPausedIntegrationEvent>(new ConsumingArgs()
            {
                Exchange = IntegrationConstants.SessionPausedConstants.Exchange,
                ExchangeType = IntegrationConstants.SessionPausedConstants.ExchangeType,
                AutoDelete = IntegrationConstants.SessionPausedConstants.AutoDelete,
                Durable = IntegrationConstants.SessionPausedConstants.Durable,
                Exclusive = IntegrationConstants.SessionPausedConstants.Exclusive,
                Queue = IntegrationConstants.SessionPausedConstants.Queue,
                RoutingKey = IntegrationConstants.SessionPausedConstants.RoutingKey,

            }, OnSessionPaused);
            var sessionCancelledConsumer = new RabbitMQConsumer(rabbitMQOptions);
            sessionCancelledConsumer.Consume<SessionCancelledIntegrationEvent>(new ConsumingArgs()
            {
                Exchange = IntegrationConstants.SessionCanceledConstants.Exchange,
                ExchangeType = IntegrationConstants.SessionCanceledConstants.ExchangeType,
                AutoDelete = IntegrationConstants.SessionCanceledConstants.AutoDelete,
                Durable = IntegrationConstants.SessionCanceledConstants.Durable,
                Exclusive = IntegrationConstants.SessionCanceledConstants.Exclusive,
                Queue = IntegrationConstants.SessionCanceledConstants.Queue,
                RoutingKey = IntegrationConstants.SessionCanceledConstants.RoutingKey,

            }, OnSessionCancelled);
            var SessionCompletedConsumer = new RabbitMQConsumer(rabbitMQOptions);
            SessionCompletedConsumer.Consume<SessionCompletedIntegrationEvent>(new ConsumingArgs()
            {
                Exchange = IntegrationConstants.SessionCompletedConstants.Exchange,
                ExchangeType = IntegrationConstants.SessionCompletedConstants.ExchangeType,
                AutoDelete = IntegrationConstants.SessionCompletedConstants.AutoDelete,
                Durable = IntegrationConstants.SessionCompletedConstants.Durable,
                Exclusive = IntegrationConstants.SessionCompletedConstants.Exclusive,
                Queue = IntegrationConstants.SessionCompletedConstants.Queue,
                RoutingKey = IntegrationConstants.SessionCompletedConstants.RoutingKey,

            }, OnSessionCompleted);
            var chunkUploadedConsumer = new RabbitMQConsumer(rabbitMQOptions);
            chunkUploadedConsumer.Consume<ChunkUploadedIntegrationEvent>(new ConsumingArgs()
            {
                Exchange = IntegrationConstants.ChunkUploadedConstants.Exchange,
                ExchangeType = IntegrationConstants.ChunkUploadedConstants.ExchangeType,
                AutoDelete = IntegrationConstants.ChunkUploadedConstants.AutoDelete,
                Durable = IntegrationConstants.ChunkUploadedConstants.Durable,
                Exclusive = IntegrationConstants.ChunkUploadedConstants.Exclusive,
                Queue = IntegrationConstants.ChunkUploadedConstants.Queue,
                RoutingKey = IntegrationConstants.ChunkUploadedConstants.RoutingKey,

            }, OnChunkUploaded);
        }

        private Task OnSessionPaused(SessionPausedIntegrationEvent sessionPausedIntegrationEvent)
        {
            memoEdit1.Invoke(() => { memoEdit1.AppendLine(sessionPausedIntegrationEvent.ToString()); });
            return Task.CompletedTask;

        }
        private Task OnSessionCancelled(SessionCancelledIntegrationEvent sessionCancelledIntegrationEvent)
        {
            memoEdit1.Invoke(() => { memoEdit1.AppendLine(sessionCancelledIntegrationEvent.ToString()); });
            return Task.CompletedTask;

        }
        private Task OnSessionCreated(SessionCreatedIntegrationEvent sessionCreatedIntegrationEvent)
        {
            memoEdit1.Invoke(() => { memoEdit1.AppendLine(sessionCreatedIntegrationEvent.ToString()); });
            return Task.CompletedTask;
        }
        private Task OnChunkUploaded(ChunkUploadedIntegrationEvent chunkUploadedIntegrationEvent)
        {
            memoEdit1.Invoke(() => { memoEdit1.AppendLine(chunkUploadedIntegrationEvent.ToString()); });
            return Task.CompletedTask;
        }
        private async Task OnSessionCompleted(SessionCompletedIntegrationEvent sessionCompletedIntegrationEvent)
        {
           await Task.Delay(2000);
            memoEdit1.Invoke(() => { memoEdit1.AppendLine(sessionCompletedIntegrationEvent.ToString()); });
            if (sessionCompletedIntegrationEvent.FileExtension.ToLower() == ".pdf")
            {
                
                pdfViewer1.Invoke(() => pdfViewer1.LoadDocument(Path.Combine(sessionCompletedIntegrationEvent.FilePath , sessionCompletedIntegrationEvent.FileName)));
                pdfViewer1.Invoke(() => pdfViewer1.ZoomMode= DevExpress.XtraPdfViewer.PdfZoomMode.PageLevel);
            }
            else
            {
                MessageBox.Show("File is not a PDF");
            }
            await Task.CompletedTask;
        }
    }



}