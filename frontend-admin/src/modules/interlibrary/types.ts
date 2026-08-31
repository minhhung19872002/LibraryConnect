/** Phân hệ liên thư viện — Z39.50, SRU và OAI-PMH. */

export type RemoteSearchField =
  | 'Any'
  | 'Title'
  | 'Author'
  | 'Isbn'
  | 'Issn'
  | 'Subject'
  | 'Publisher';

export interface Z3950TargetDto {
  id: string;
  name: string;
  host: string;
  port: number;
  databaseName: string;
  username: string | null;
  charset: string;
  recordSyntax: string;
  timeoutSeconds: number;
  useSru: boolean;
  sruBaseUrl: string | null;
  isActive: boolean;
  showOnOpac: boolean;
  sortOrder: number;
  lastCheckedAt: string | null;
  lastCheckOk: boolean | null;
  lastCheckMessage: string | null;
}

export interface Z3950CheckResultDto {
  success: boolean;
  message: string;
  durationMs: number;
  serverName: string | null;
  serverVersion: string | null;
  sampleHits: number | null;
}

export interface RemoteRecordDto {
  targetId: string;
  targetName: string;
  position: number;
  controlNumber: string | null;
  title: string | null;
  author: string | null;
  publisher: string | null;
  publishYear: string | null;
  isbn: string | null;
  edition: string | null;
  pages: string | null;
  marcJson: string;
  existingBibId: string | null;
  existingBibTitle: string | null;
}

export interface RemoteSearchTargetResultDto {
  targetId: string;
  targetName: string;
  success: boolean;
  message: string | null;
  totalHits: number;
  durationMs: number;
  records: RemoteRecordDto[];
}

export interface RemoteSearchResultDto {
  targets: RemoteSearchTargetResultDto[];
  totalHits: number;
  totalRecords: number;
}

export interface Z3950SearchLogDto {
  id: string;
  targetId: string | null;
  targetName: string | null;
  query: string;
  resultCount: number;
  durationMs: number;
  success: boolean;
  message: string | null;
  occurredAt: string;
}

export interface OaiRepositoryDto {
  id: string;
  name: string;
  baseUrl: string;
  metadataPrefix: string;
  setSpec: string | null;
  scheduleCron: string | null;
  isActive: boolean;
  defaultDocumentTypeId: string | null;
  defaultDocumentTypeName: string | null;
  lastHarvestAt: string | null;
  resumptionToken: string | null;
}

export interface OaiHarvestLogDto {
  id: string;
  repositoryId: string;
  repositoryName: string;
  startedAt: string;
  finishedAt: string | null;
  recordsFetched: number;
  recordsImported: number;
  recordsSkipped: number;
  status: string;
  errors: string | null;
}

export interface OaiIdentifyDto {
  repositoryName: string;
  baseUrl: string;
  protocolVersion: string;
  earliestDatestamp: string | null;
  adminEmail: string | null;
  metadataPrefixes: string[];
  sets: string[];
}
