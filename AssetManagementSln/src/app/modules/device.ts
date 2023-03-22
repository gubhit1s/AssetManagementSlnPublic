import { DeviceType } from './device-type';

export interface Device {
    id: number,
    serviceTag: string,
    deviceName: string | null,
    acquiredDate: string | null,
    deviceModel: string | null,
    poNumber: string | null,
    deviceTypeId: string,
    deviceTypeName: string,
    deviceStatusId: string,
    deviceStatusName: string,
    currentUsername: string | null,
    lastUser1: string | null,
    lastUser2: string | null,
}