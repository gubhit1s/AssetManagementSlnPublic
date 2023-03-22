import { Device } from './device';
import { DeviceType } from './device-type';

export interface User {
    userName: string,
    displayName: string,
    email: string,
    office: string,
    devices?: Device[],
    devicesUnidentified?: DeviceType[],
    checked: boolean
}