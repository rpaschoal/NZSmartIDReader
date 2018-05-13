using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Microsoft.Rest;

namespace NZSmartIDReader.Core
{
    public sealed class NZDriverLicenceSmartIdReader : SmartIdReader, ISmartIdReader
    {
        private readonly Guid GeneralCompactDomainId = new Guid("0732100f-1a38-4e49-a514-c9b44c697ab5");

        private Project _customVisionProject;
        private Project CustomVisionProject {
            get
            {
                if (_customVisionProject == null)
                {
                    var existingProjects = TrainingApi.GetProjects();
                    var existingProject = existingProjects.FirstOrDefault(p => p.Name.Equals(nameof(NZDriverLicenceSmartIdReader)));

                    if (existingProject == null)
                    {
                        _customVisionProject = TrainingApi.CreateProject(nameof(NZDriverLicenceSmartIdReader), domainId: GeneralCompactDomainId);
                    }
                    else
                    {
                        _customVisionProject = existingProject;
                    }
                }

                return _customVisionProject;
            }
        }

        private TrainingApi TrainingApi { get; set; } = new TrainingApi() { ApiKey = AzureCredentials.TrainingKey };

        public NZDriverLicenceSmartIdReader(string samplesDirectory, double predictionThreshold) : base(samplesDirectory, predictionThreshold) {}

        private (Tag tag1, Tag tag2) GetTags()
        {
            var projectTags = TrainingApi.GetTags(CustomVisionProject.Id);

            var tag = projectTags.Tags.FirstOrDefault(t => t.Name.Equals("DriverLicence")) ?? TrainingApi.CreateTag(CustomVisionProject.Id, "DriverLicence");
            var tag2 = projectTags.Tags.FirstOrDefault(t => t.Name.Equals("NZDriverLicence")) ?? TrainingApi.CreateTag(CustomVisionProject.Id, "NZDriverLicence");

            return (tag, tag2);
        }

        internal override void Train(string samplesDirectory)
        {
            var tags = GetTags();

            // Batch upload files
            var sampleImagesNZ = Directory.GetFiles(Path.Combine(samplesDirectory, "NewZealand")).Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img)));
            var sampleImagesGlobal = Directory.GetFiles(Path.Combine(samplesDirectory, "Global")).Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img)));

            TrainingApi.CreateImagesFromFiles(CustomVisionProject.Id, new ImageFileCreateBatch(sampleImagesNZ.ToList(), new List<Guid>() { tags.tag1.Id, tags.tag2.Id }));
            TrainingApi.CreateImagesFromFiles(CustomVisionProject.Id, new ImageFileCreateBatch(sampleImagesGlobal.ToList(), new List<Guid>() { tags.tag1.Id }));

            try
            {
                var trainingIteration = TrainingApi.TrainProject(CustomVisionProject.Id);

                // TODO: Improve this: make the initialization async and add a timeout
                while (trainingIteration.Status == "Training")
                {
                    Thread.Sleep(1000);

                    // Re-query the iteration to get it's updated status
                    trainingIteration = TrainingApi.GetIteration(CustomVisionProject.Id, trainingIteration.Id);
                }

                trainingIteration.IsDefault = true;
                TrainingApi.UpdateIteration(CustomVisionProject.Id, trainingIteration.Id, trainingIteration);
            }
            catch (HttpOperationException e) when (e.Response.Content.Contains("BadRequestTrainingNotNeeded"))
            {
            }
        }

        public bool IsValid(Stream imageData)
        {
            if (IsInitialized)
            {
                var predictionEndpoint = new PredictionEndpoint() { ApiKey = AzureCredentials.PredictionKey };

                var result = predictionEndpoint.PredictImage(CustomVisionProject.Id, imageData);

                var overallProbability = result.Predictions.Average(x => x.Probability);

                return overallProbability >= PredictionThreashold;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(NZDriverLicenceSmartIdReader)} is not yet initialized. You can monitor the status of the smart reader via the ${nameof(IsInitialized)} property.");
            }
        }

        public string Read()
        {
            throw new NotImplementedException();
        }
    }
}
