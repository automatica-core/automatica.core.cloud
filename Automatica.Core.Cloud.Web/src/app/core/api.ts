import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

// Matches Automatica.Core.Cloud.WebApi/Dtos/AdminDtos.cs (Newtonsoft, camelCase).

// roles travel as strings (StringEnumConverter)
export type UserRole = 'Nothing' | 'Viewer' | 'Manager' | 'Administrator' | 'SystemAdministrator';
export const userRoles: UserRole[] = ['Viewer', 'Manager', 'Administrator', 'SystemAdministrator', 'Nothing'];
const roleRank: Record<UserRole, number> = { Nothing: 0, Viewer: 1, Manager: 2, Administrator: 3, SystemAdministrator: 4 };
export const roleAtLeast = (role: UserRole | undefined, min: UserRole): boolean => !!role && roleRank[role] >= roleRank[min];

export const roleNames: Record<UserRole, string> = {
  Nothing: 'No access',
  Viewer: 'Viewer',
  Manager: 'Manager',
  Administrator: 'Administrator',
  SystemAdministrator: 'System administrator',
};

export interface User {
  objId: string;
  userName: string;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  publisherName: string | null;
  role: UserRole;
  enabled: boolean;
  hasPassword: boolean;
  lastLoginAt: string | null;
  lockedUntil: string | null;
}

export interface UserSave {
  userName: string;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  publisherName: string | null;
  role: UserRole;
  enabled: boolean;
  password?: string | null;
}

export interface UserCreated {
  user: User;
  initialPassword: string | null;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface LicenseSummary {
  objId: string;
  expiresAt: string;
  licensedTo: string;
  allowRemoteControl: boolean;
  allowTextToSpeech: boolean;
  maxDatapoints: number;
  maxUsers: number;
}

export interface CoreServer {
  objId: string;
  serverGuid: string;
  serverName: string;
  ownerId: string;
  ownerName: string | null;
  version: string | null;
  rid: string | null;
  lastKnownConnection: string | null;
  lastKnownRemoteConnectUrl: string | null;
  lastKnownRemoteConnectUrlDate: string | null;
  apiKeyCreatedAt: string | null;
  hasApiKey: boolean;
  license: LicenseSummary | null;
}

export interface CoreServerSecret {
  server: CoreServer;
  apiKey: string;
}

export interface MailRecipient {
  objId: string;
  email: string;
  verified: boolean;
  verifiedAt: string | null;
  createdAt: string;
  isOwner: boolean;
}

export interface License {
  objId: string;
  coreServerId: string;
  serverName: string | null;
  serverGuid: string | null;
  ownerName: string | null;
  expiresAt: string;
  maxDatapoints: number;
  maxUsers: number;
  licensedTo: string;
  email: string | null;
  allowRemoteControl: boolean;
  maxRemoteTunnels: number;
  maxRecordingDataPoints: number;
  maxSatellites: number;
  allowTextToSpeech: boolean;
  features: string[];
}

export type GenerateLicense = Omit<License, 'objId' | 'serverName' | 'serverGuid' | 'ownerName'>;

export interface Plugin {
  objId: string;
  pluginGuid: string;
  name: string;
  componentName: string;
  version: string;
  minCoreServerVersion: string;
  branch: string;
  pluginType: string;
  publisher: string | null;
  isPublic: boolean;
  isPrerelease: boolean;
  azureUrl: string;
  features: string[];
}

export interface ServerVersion {
  objId: string;
  version: string;
  rid: string;
  branch: string;
  changeLog: string | null;
  isPreRelease: boolean;
  isPublic: boolean;
  azureUrl: string;
}

export interface DockerVersion {
  objId?: string;
  version: string;
  imageName: string;
  imageTag: string;
  branch: string;
  changeLog: string | null;
  isPreRelease: boolean;
  isPublic: boolean;
}

export type DeployScope = 'DeployPlugin' | 'DeployUpdate' | 'DeployDocker' | 'ReadPlugins';
export const deployScopes: DeployScope[] = ['DeployPlugin', 'DeployUpdate', 'DeployDocker', 'ReadPlugins'];

export interface DeployKey {
  objId: string;
  name: string;
  keyPrefix: string;
  scopes: DeployScope[];
  allowedBranches: string;
  publisherUserId: string;
  publisherUserName: string | null;
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string | null;
  lastUsedIp: string | null;
  revokedAt: string | null;
}

export interface DeployKeyCreate {
  name: string;
  scopes: DeployScope[];
  allowedBranches: string;
  publisherUserId: string | null;
  expiresInDays: number | null;
}

export interface DeployKeyCreated {
  deployKey: DeployKey;
  key: string;
}

const base = '/webapi/v1';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  // servers
  servers(mine = false): Observable<CoreServer[]> {
    return this.http.get<CoreServer[]>(`${base}/coreServer`, { params: { mine } });
  }
  createServer(data: { serverName: string; serverGuid?: string | null; ownerId?: string | null }): Observable<CoreServerSecret> {
    return this.http.post<CoreServerSecret>(`${base}/coreServer`, data);
  }
  updateServer(id: string, data: { serverName: string; ownerId?: string | null }): Observable<CoreServer> {
    return this.http.put<CoreServer>(`${base}/coreServer/${id}`, data);
  }
  rotateKey(id: string): Observable<CoreServerSecret> {
    return this.http.post<CoreServerSecret>(`${base}/coreServer/${id}/rotateKey`, null);
  }
  deleteServer(id: string): Observable<void> {
    return this.http.delete<void>(`${base}/coreServer/${id}`);
  }
  mailRecipients(id: string): Observable<MailRecipient[]> {
    return this.http.get<MailRecipient[]>(`${base}/coreServer/${id}/mailRecipients`);
  }
  addMailRecipient(id: string, email: string): Observable<MailRecipient> {
    return this.http.post<MailRecipient>(`${base}/coreServer/${id}/mailRecipients`, { email });
  }
  resendVerification(id: string, recipientId: string): Observable<void> {
    return this.http.post<void>(`${base}/coreServer/${id}/mailRecipients/${recipientId}/resend`, null);
  }
  deleteMailRecipient(id: string, recipientId: string): Observable<void> {
    return this.http.delete<void>(`${base}/coreServer/${id}/mailRecipients/${recipientId}`);
  }
  verifyMail(token: string): Observable<void> {
    return this.http.post<void>(`${base}/mail/verify`, { token });
  }

  // licenses
  licenses(): Observable<License[]> {
    return this.http.get<License[]>(`${base}/license`);
  }
  licenseFeatures(): Observable<string[]> {
    return this.http.get<string[]>(`${base}/license/features`);
  }
  licenseKey(id: string): Observable<string> {
    return this.http.get(`${base}/license/${id}/key`, { responseType: 'text' });
  }
  generateLicense(data: GenerateLicense): Observable<License> {
    return this.http.post<License>(`${base}/license`, data);
  }
  deleteLicense(id: string): Observable<void> {
    return this.http.delete<void>(`${base}/license/${id}`);
  }

  // publishing
  plugins(): Observable<Plugin[]> {
    return this.http.get<Plugin[]>(`${base}/plugins`);
  }
  uploadPlugin(file: File, branch: string): Observable<Plugin> {
    return this.http.post<Plugin>(`${base}/plugins/upload`, formData(file, branch));
  }
  updatePlugin(id: string, data: { isPublic: boolean; isPrerelease: boolean }): Observable<Plugin> {
    return this.http.put<Plugin>(`${base}/plugins/${id}`, data);
  }
  deletePlugin(id: string): Observable<void> {
    return this.http.delete<void>(`${base}/plugins/${id}`);
  }
  serverVersions(): Observable<ServerVersion[]> {
    return this.http.get<ServerVersion[]>(`${base}/coreServerVersion`);
  }
  uploadServerVersion(file: File, branch: string): Observable<ServerVersion> {
    return this.http.post<ServerVersion>(`${base}/coreServerVersion/upload`, formData(file, branch));
  }
  updateServerVersion(id: string, data: { changeLog: string | null; isPreRelease: boolean; isPublic: boolean }): Observable<ServerVersion> {
    return this.http.put<ServerVersion>(`${base}/coreServerVersion/${id}`, data);
  }
  deleteServerVersion(id: string): Observable<void> {
    return this.http.delete<void>(`${base}/coreServerVersion/${id}`);
  }
  dockerVersions(): Observable<DockerVersion[]> {
    return this.http.get<DockerVersion[]>(`${base}/coreServerDockerVersion`);
  }
  saveDockerVersion(data: DockerVersion): Observable<DockerVersion> {
    return data.objId
      ? this.http.put<DockerVersion>(`${base}/coreServerDockerVersion/${data.objId}`, data)
      : this.http.post<DockerVersion>(`${base}/coreServerDockerVersion`, data);
  }
  deleteDockerVersion(id: string): Observable<void> {
    return this.http.delete<void>(`${base}/coreServerDockerVersion/${id}`);
  }
  deployKeys(): Observable<DeployKey[]> {
    return this.http.get<DeployKey[]>(`${base}/deployKeys`);
  }
  createDeployKey(data: DeployKeyCreate): Observable<DeployKeyCreated> {
    return this.http.post<DeployKeyCreated>(`${base}/deployKeys`, data);
  }
  revokeDeployKey(id: string): Observable<DeployKey> {
    return this.http.post<DeployKey>(`${base}/deployKeys/${id}/revoke`, null);
  }

  // users
  users(): Observable<User[]> {
    return this.http.get<User[]>(`${base}/users`);
  }
  createUser(data: UserSave): Observable<UserCreated> {
    return this.http.post<UserCreated>(`${base}/users`, data);
  }
  updateUser(id: string, data: UserSave): Observable<User> {
    return this.http.put<User>(`${base}/users/${id}`, data);
  }
  resetPassword(id: string, newPassword: string | null): Observable<UserCreated> {
    return this.http.post<UserCreated>(`${base}/users/${id}/password`, { newPassword });
  }
  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${base}/users/${id}`);
  }
}

function formData(file: File, branch: string): FormData {
  const data = new FormData();
  data.append('file', file, file.name);
  data.append('branch', branch);
  return data;
}

/** Readable message from an HttpErrorResponse ({ error: "code" } from the api, or the status). */
export function errorMessage(err: unknown): string {
  const e = err as { status?: number; error?: unknown };
  const body = e?.error as { error?: string; errors?: Record<string, string[]> } | string | undefined;
  if (typeof body === 'object' && body?.error) {
    return body.error.replace(/-/g, ' ');
  }
  if (typeof body === 'object' && body?.errors) {
    return Object.values(body.errors).flat().join(' ');
  }
  switch (e?.status) {
    case 0:
      return 'The cloud is not reachable.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You are not allowed to do this.';
    case 404:
      return 'Not found.';
    case 409:
      return 'This already exists.';
    case 429:
      return 'Too many attempts. Please wait a minute.';
    default:
      return 'Something went wrong.';
  }
}
