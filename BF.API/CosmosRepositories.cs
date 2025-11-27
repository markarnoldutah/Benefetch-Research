using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Ec.Api.Contracts;
using Ec.Api.Persistence;
using Microsoft.Azure.Cosmos;

namespace Ec.Api.Persistence.Cosmos
{
    // =====================================================
    // Helpers
    // =====================================================

    public abstract class CosmosRepositoryBase
    {
        protected readonly CosmosClient Client;

        protected CosmosRepositoryBase(CosmosClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected Container GetContainer(string databaseId, string containerId)
        {
            if (string.IsNullOrWhiteSpace(databaseId)) throw new ArgumentException("Required", nameof(databaseId));
            if (string.IsNullOrWhiteSpace(containerId)) throw new ArgumentException("Required", nameof(containerId));

            return Client.GetContainer(databaseId, containerId);
        }
    }

    // =====================================================
    // Practices
    // =====================================================

    public class CosmosPracticeRepository : CosmosRepositoryBase, IPracticeRepository
    {
        private readonly Container _container;

        public CosmosPracticeRepository(
            CosmosClient client,
            string databaseId,
            string containerId) : base(client)
        {
            _container = GetContainer(databaseId, containerId);
        }

        public async Task<List<PracticeEntity>> GetPracticesForTenantAsync(string tenantId, bool includeLocations)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var sql = "SELECT * FROM c WHERE c.tenantId = @tenantId";
            var query = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId);

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };

            var iterator = _container.GetItemQueryIterator<PracticeEntity>(query, requestOptions: options);
            var results = new List<PracticeEntity>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            // You can strip locations if !includeLocations here
            if (!includeLocations)
            {
                foreach (var p in results)
                {
                    // p.Locations = null; // if you want to trim payload
                }
            }

            return results;
        }

        public async Task<PracticeEntity?> GetByIdAsync(string tenantId, string practiceId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(practiceId))
                throw new ArgumentException("practiceId is required.", nameof(practiceId));

            try
            {
                var resp = await _container.ReadItemAsync<PracticeEntity>(
                    id: practiceId,
                    partitionKey: new PartitionKey(tenantId));

                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }

    // =====================================================
    // Payers
    // =====================================================

    public class CosmosPayerRepository : CosmosRepositoryBase, IPayerRepository
    {
        private readonly Container _container;

        public CosmosPayerRepository(
            CosmosClient client,
            string databaseId,
            string containerId) : base(client)
        {
            _container = GetContainer(databaseId, containerId);
        }

        public async Task<List<PayerEntity>> SearchAsync(string tenantId, string? planType, string? search)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var sql = "SELECT * FROM c WHERE c.tenantId = @tenantId";
            var queryDef = new QueryDefinition(sql).WithParameter("@tenantId", tenantId);

            if (!string.IsNullOrWhiteSpace(planType))
            {
                sql += " AND ARRAY_CONTAINS(c.planTypes, @planType)";
                queryDef = new QueryDefinition(sql)
                    .WithParameter("@tenantId", tenantId)
                    .WithParameter("@planType", planType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND CONTAINS(LOWER(c.name), @search)";
                queryDef = new QueryDefinition(sql)
                    .WithParameter("@tenantId", tenantId)
                    .WithParameter("@search", search.ToLowerInvariant());
            }

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };

            var iterator = _container.GetItemQueryIterator<PayerEntity>(queryDef, requestOptions: options);
            var results = new List<PayerEntity>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        public async Task<PayerEntity?> GetByIdAsync(string tenantId, string payerId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(payerId))
                throw new ArgumentException("payerId is required.", nameof(payerId));

            try
            {
                var resp = await _container.ReadItemAsync<PayerEntity>(
                    id: payerId,
                    partitionKey: new PartitionKey(tenantId));

                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }

    // =====================================================
    // Patients
    // =====================================================

    public class CosmosPatientRepository : CosmosRepositoryBase, IPatientRepository
    {
        private readonly Container _container;

        public CosmosPatientRepository(
            CosmosClient client,
            string databaseId,
            string containerId) : base(client)
        {
            _container = GetContainer(databaseId, containerId);
        }

        public async Task<PatientEntity?> GetByIdAsync(string tenantId, string patientId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));

            try
            {
                var response = await _container.ReadItemAsync<PatientEntity>(
                    id: patientId,
                    partitionKey: new PartitionKey(tenantId));

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task CreateAsync(PatientEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));

            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = Guid.NewGuid().ToString("n");

            await _container.CreateItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task UpdateAsync(PatientEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Id))
                throw new ArgumentException("Id must be set on entity.", nameof(entity));

            await _container.UpsertItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task<PagedResult<PatientSearchResultDto>> SearchAsync(
            string tenantId,
            PatientSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            if (request.PageSize <= 0)
                throw new ArgumentException("PageSize must be > 0.", nameof(request));

            var sql = "SELECT c.id, c.tenantId, c.firstName, c.lastName, c.dateOfBirth " +
                      "FROM c WHERE c.tenantId = @tenantId";

            var queryDef = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId);

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                sql += " AND (CONTAINS(LOWER(c.firstName), @search) OR CONTAINS(LOWER(c.lastName), @search))";
                queryDef = new QueryDefinition(sql)
                    .WithParameter("@tenantId", tenantId)
                    .WithParameter("@search", request.SearchText!.ToLowerInvariant());
            }

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId),
                MaxItemCount = request.PageSize
            };

            var iterator = _container.GetItemQueryIterator<PatientEntity>(
                queryDef,
                continuationToken: request.ContinuationToken,
                requestOptions: options);

            var items = new List<PatientSearchResultDto>();
            string? newToken = null;

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                newToken = response.ContinuationToken;

                foreach (var e in response.Resource)
                {
                    items.Add(new PatientSearchResultDto
                    {
                        PatientId = e.Id,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        DateOfBirth = e.DateOfBirth
                    });
                }
            }

            return new PagedResult<PatientSearchResultDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = null,
                ContinuationToken = newToken
            };
        }
    }

    // =====================================================
    // Coverage (CoverageEnrollments)
    // =====================================================

    public class CosmosCoverageRepository : CosmosRepositoryBase, ICoverageRepository
    {
        private readonly Container _container;

        public CosmosCoverageRepository(
            CosmosClient client,
            string databaseId,
            string containerId) : base(client)
        {
            _container = GetContainer(databaseId, containerId);
        }

        public async Task<CoverageEnrollmentEntity?> GetByIdAsync(
            string tenantId,
            string patientId,
            string coverageEnrollmentId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(coverageEnrollmentId))
                throw new ArgumentException("coverageEnrollmentId is required.", nameof(coverageEnrollmentId));

            try
            {
                var resp = await _container.ReadItemAsync<CoverageEnrollmentEntity>(
                    id: coverageEnrollmentId,
                    partitionKey: new PartitionKey(tenantId));

                // Optional double-check on patient
                if (!string.Equals(resp.Resource.PatientId, patientId, StringComparison.OrdinalIgnoreCase))
                    return null;

                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task CreateAsync(CoverageEnrollmentEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));

            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = Guid.NewGuid().ToString("n");

            await _container.CreateItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task UpdateAsync(CoverageEnrollmentEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Id))
                throw new ArgumentException("Id must be set on entity.", nameof(entity));

            await _container.UpsertItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task DeleteAsync(CoverageEnrollmentEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Id))
                throw new ArgumentException("Id must be set on entity.", nameof(entity));

            try
            {
                await _container.DeleteItemAsync<CoverageEnrollmentEntity>(
                    id: entity.Id,
                    partitionKey: new PartitionKey(entity.TenantId));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Swallow here; service will handle "not found" logic by its own Get call
            }
        }
    }

    // =====================================================
    // Encounters + Eligibility Checks
    // =====================================================

    public class CosmosEncounterRepository : CosmosRepositoryBase, IEncounterRepository
    {
        private readonly Container _encounterContainer;
        private readonly Container _eligibilityContainer;

        public CosmosEncounterRepository(
            CosmosClient client,
            string databaseId,
            string encounterContainerId,
            string eligibilityContainerId) : base(client)
        {
            _encounterContainer = GetContainer(databaseId, encounterContainerId);
            _eligibilityContainer = GetContainer(databaseId, eligibilityContainerId);
        }

        public async Task<EncounterEntity?> GetByIdAsync(string tenantId, string encounterId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            try
            {
                var resp = await _encounterContainer.ReadItemAsync<EncounterEntity>(
                    id: encounterId,
                    partitionKey: new PartitionKey(tenantId));
                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task CreateAsync(EncounterEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = Guid.NewGuid().ToString("n");

            await _encounterContainer.CreateItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task UpdateAsync(EncounterEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set on entity.", nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Id))
                throw new ArgumentException("Id must be set on entity.", nameof(entity));

            await _encounterContainer.UpsertItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task<PagedResult<EncounterSummaryDto>> SearchForPatientAsync(
            string tenantId,
            string patientId,
            PatientEncounterSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));
            if (request.PageSize <= 0)
                throw new ArgumentException("PageSize must be > 0.", nameof(request));

            var sql = "SELECT c.id, c.tenantId, c.patientId, c.visitDate, c.visitTypeCode " +
                      "FROM c WHERE c.tenantId = @tenantId AND c.patientId = @patientId";

            var queryDef = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId)
                .WithParameter("@patientId", patientId);

            if (request.FromDate.HasValue)
            {
                sql += " AND c.visitDate >= @fromDate";
                queryDef = new QueryDefinition(sql)
                    .WithParameter("@tenantId", tenantId)
                    .WithParameter("@patientId", patientId)
                    .WithParameter("@fromDate", request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                sql += " AND c.visitDate <= @toDate";
                queryDef = new QueryDefinition(sql)
                    .WithParameter("@tenantId", tenantId)
                    .WithParameter("@patientId", patientId)
                    .WithParameter("@toDate", request.ToDate.Value);
            }

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId),
                MaxItemCount = request.PageSize
            };

            var iterator = _encounterContainer.GetItemQueryIterator<EncounterEntity>(
                queryDef,
                continuationToken: request.ContinuationToken,
                requestOptions: options);

            var items = new List<EncounterSummaryDto>();
            string? newToken = null;

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                newToken = response.ContinuationToken;

                foreach (var e in response.Resource)
                {
                    items.Add(new EncounterSummaryDto
                    {
                        EncounterId = e.Id,
                        PatientId = e.PatientId,
                        VisitDate = e.VisitDate,
                        VisitTypeCode = e.VisitTypeCode
                    });
                }
            }

            return new PagedResult<EncounterSummaryDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = null,
                ContinuationToken = newToken
            };
        }

        public async Task<PagedResult<EncounterSummaryDto>> SearchAsync(
            string tenantId,
            EncounterSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (request.PageSize <= 0)
                throw new ArgumentException("PageSize must be > 0.", nameof(request));

            var sql = "SELECT c.id, c.tenantId, c.patientId, c.visitDate, c.visitTypeCode " +
                      "FROM c WHERE c.tenantId = @tenantId";

            var queryDef = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId);

            if (!string.IsNullOrWhiteSpace(request.PatientId))
            {
                sql += " AND c.patientId = @patientId";
                queryDef = new QueryDefinition(sql)
                    .WithParameter("@tenantId", tenantId)
                    .WithParameter("@patientId", request.PatientId);
            }

            // add more filters as needed

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId),
                MaxItemCount = request.PageSize
            };

            var iterator = _encounterContainer.GetItemQueryIterator<EncounterEntity>(
                queryDef,
                continuationToken: request.ContinuationToken,
                requestOptions: options);

            var items = new List<EncounterSummaryDto>();
            string? newToken = null;

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                newToken = response.ContinuationToken;

                foreach (var e in response.Resource)
                {
                    items.Add(new EncounterSummaryDto
                    {
                        EncounterId = e.Id,
                        PatientId = e.PatientId,
                        VisitDate = e.VisitDate,
                        VisitTypeCode = e.VisitTypeCode
                    });
                }
            }

            return new PagedResult<EncounterSummaryDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = null,
                ContinuationToken = newToken
            };
        }

        // ---------- Eligibility Checks ----------

        public async Task<EligibilityCheckEntity> CreateEligibilityCheckAsync(
            string tenantId,
            string encounterId,
            EligibilityCheckRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            var entity = new EligibilityCheckEntity
            {
                Id = Guid.NewGuid().ToString("n"),
                TenantId = tenantId,
                EncounterId = encounterId,
                CoverageEnrollmentId = request.CoverageEnrollmentId,
                RequestTimestampUtc = DateTime.UtcNow
                // Response fields populated after clearinghouse call
            };

            var resp = await _eligibilityContainer.CreateItemAsync(
                entity,
                partitionKey: new PartitionKey(tenantId));

            return resp.Resource;
        }

        public async Task<List<EligibilityCheckSummaryDto>> GetEligibilityChecksAsync(
            string tenantId,
            string encounterId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            var sql = "SELECT c.id, c.tenantId, c.encounterId, c.requestTimestampUtc, c.responseTimestampUtc " +
                      "FROM c WHERE c.tenantId = @tenantId AND c.encounterId = @encounterId";

            var queryDef = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId)
                .WithParameter("@encounterId", encounterId);

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };

            var iterator = _eligibilityContainer.GetItemQueryIterator<EligibilityCheckEntity>(
                queryDef,
                requestOptions: options);

            var list = new List<EligibilityCheckSummaryDto>();

            while (iterator.HasMoreResults)
            {
                var resp = await iterator.ReadNextAsync();
                foreach (var e in resp.Resource)
                {
                    list.Add(new EligibilityCheckSummaryDto
                    {
                        EligibilityCheckId = e.Id,
                        EncounterId = e.EncounterId,
                        RequestTimestampUtc = e.RequestTimestampUtc,
                        ResponseTimestampUtc = e.ResponseTimestampUtc
                    });
                }
            }

            return list;
        }

        public async Task<EligibilityCheckEntity?> GetEligibilityCheckAsync(
            string tenantId,
            string encounterId,
            string eligibilityCheckId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(eligibilityCheckId))
                throw new ArgumentException("eligibilityCheckId is required.", nameof(eligibilityCheckId));

            try
            {
                var resp = await _eligibilityContainer.ReadItemAsync<EligibilityCheckEntity>(
                    id: eligibilityCheckId,
                    partitionKey: new PartitionKey(tenantId));

                if (!string.Equals(resp.Resource.EncounterId, encounterId, StringComparison.OrdinalIgnoreCase))
                    return null;

                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }

    // =====================================================
    // Lookups
    // =====================================================

    public class CosmosLookupRepository : CosmosRepositoryBase, ILookupRepository
    {
        private readonly Container _container;

        public CosmosLookupRepository(
            CosmosClient client,
            string databaseId,
            string containerId) : base(client)
        {
            _container = GetContainer(databaseId, containerId);
        }

        public async Task<List<LookupItemDto>> GetVisitTypesAsync()
        {
            var sql = "SELECT c.code, c.displayName FROM c WHERE c.type = 'VisitType'";
            var queryDef = new QueryDefinition(sql);

            var iterator = _container.GetItemQueryIterator<LookupItemDto>(queryDef);
            var list = new List<LookupItemDto>();

            while (iterator.HasMoreResults)
            {
                var resp = await iterator.ReadNextAsync();
                list.AddRange(resp.Resource);
            }

            return list;
        }

        public async Task<List<LookupItemDto>> GetCobReasonsAsync()
        {
            var sql = "SELECT c.code, c.displayName FROM c WHERE c.type = 'CobReason'";
            var queryDef = new QueryDefinition(sql);

            var iterator = _container.GetItemQueryIterator<LookupItemDto>(queryDef);
            var list = new List<LookupItemDto>();

            while (iterator.HasMoreResults)
            {
                var resp = await iterator.ReadNextAsync();
                list.AddRange(resp.Resource);
            }

            return list;
        }

        public async Task<List<VisitTypeServiceTypesDto>> GetVisitTypeServiceTypesAsync()
        {
            var sql = "SELECT c.visitTypeCode, c.serviceTypeCodes FROM c WHERE c.type = 'VisitTypeServiceTypes'";
            var queryDef = new QueryDefinition(sql);

            var iterator = _container.GetItemQueryIterator<VisitTypeServiceTypesDto>(queryDef);
            var list = new List<VisitTypeServiceTypesDto>();

            while (iterator.HasMoreResults)
            {
                var resp = await iterator.ReadNextAsync();
                list.AddRange(resp.Resource);
            }

            return list;
        }
    }

    // =====================================================
    // Config
    // =====================================================

    public class CosmosConfigRepository : CosmosRepositoryBase, IConfigRepository
    {
        private readonly Container _container;

        public CosmosConfigRepository(
            CosmosClient client,
            string databaseId,
            string containerId) : base(client)
        {
            _container = GetContainer(databaseId, containerId);
        }

        public async Task<TenantConfigEntity?> GetTenantConfigAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            try
            {
                var resp = await _container.ReadItemAsync<TenantConfigEntity>(
                    id: tenantId,
                    partitionKey: new PartitionKey(tenantId));

                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task SaveTenantConfigAsync(TenantConfigEntity entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.TenantId))
                throw new ArgumentException("TenantId must be set.", nameof(entity));

            await _container.UpsertItemAsync(
                entity,
                partitionKey: new PartitionKey(entity.TenantId));
        }

        public async Task<List<PayerConfigDto>> GetPayerConfigsAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var sql = "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'PayerConfig'";
            var queryDef = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId);

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };

            var iterator = _container.GetItemQueryIterator<PayerConfigDto>(queryDef, requestOptions: options);
            var list = new List<PayerConfigDto>();

            while (iterator.HasMoreResults)
            {
                var resp = await iterator.ReadNextAsync();
                list.AddRange(resp.Resource);
            }

            return list;
        }

        public async Task<PayerConfigDto?> GetPayerConfigAsync(string tenantId, string payerId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(payerId))
                throw new ArgumentException("payerId is required.", nameof(payerId));

            var sql = "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'PayerConfig' AND c.payerId = @payerId";
            var queryDef = new QueryDefinition(sql)
                .WithParameter("@tenantId", tenantId)
                .WithParameter("@payerId", payerId);

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId),
                MaxItemCount = 1
            };

            var iterator = _container.GetItemQueryIterator<PayerConfigDto>(queryDef, requestOptions: options);

            if (!iterator.HasMoreResults)
                return null;

            var resp = await iterator.ReadNextAsync();
            return resp.Resource.FirstOrDefault();
        }

        public async Task SavePayerConfigAsync(PayerConfigDto config)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.TenantId))
                throw new ArgumentException("TenantId must be set.", nameof(config));
            if (string.IsNullOrWhiteSpace(config.PayerId))
                throw new ArgumentException("PayerId must be set.", nameof(config));

            // You can use a dedicated entity or store directly as DTO
            await _container.UpsertItemAsync(
                config,
                partitionKey: new PartitionKey(config.TenantId));
        }
    }

    // =====================================================
    // TenantAccessService (example)
    // =====================================================

    public class CosmosTenantAccessService : ITenantAccessService
    {
        private readonly Container _container;

        public CosmosTenantAccessService(
            CosmosClient client,
            string databaseId,
            string containerId)
        {
            _container = client.GetContainer(databaseId, containerId);
        }

        public async Task<bool> HasAccessAsync(string userId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var sql = "SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId AND c.tenantId = @tenantId";
            var queryDef = new QueryDefinition(sql)
                .WithParameter("@userId", userId)
                .WithParameter("@tenantId", tenantId);

            var options = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId)
            };

            var iterator = _container.GetItemQueryIterator<int>(queryDef, requestOptions: options);
            if (!iterator.HasMoreResults) return false;

            var resp = await iterator.ReadNextAsync();
            var count = resp.Resource.FirstOrDefault();
            return count > 0;
        }

        public IReadOnlyList<string> GetRolesForUser(ClaimsPrincipal user)
        {
            // Could be from claims or a separate repo call.
            // For now just map claims of type "role".
            var roles = user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return roles;
        }
    }
}
