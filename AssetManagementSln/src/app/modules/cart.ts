import { Device } from "./device";
import { DeviceUnidentified } from "./device-unidentified";

export interface CurrentUserCart {
    currentDevicesIdentified: Device[],
    currentDevicesUnidentified: DeviceUnidentified[],
    count: number;
}

export interface CartDetail {
    itemId: number,
    deviceTypeId: number,
    deviceTypeName: string,
    deviceId?: number,
    serviceTag?: string,
    deviceName?: string | null
}

export interface DeviceTypesOfCart {
    userName?: string,
    deviceTypeIds: number[]
}