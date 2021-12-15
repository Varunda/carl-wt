﻿import * as axios from "axios";
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class FacilityControlEntry {
	public facilityID: number = 0;
	public facilityName: string = "";
	public typeID: number = 0;
	public typeName: string = "";
	public zoneID: number = 0;
	public captured: number = 0;
	public captureAverage: number = 0;
	public defended: number = 0;
	public defenseAverage: number = 0;
	public totalAverage: number = 0;

	public ratio: number = 0;
	public total: number = 0;
}

export class LedgerOptions {
	public zoneID: number | null = null;
	public worldID: number[] = [];
	public playerThreshold: number | null = null;
}

export class LedgerApi extends ApiWrapper<FacilityControlEntry> {
	private static _instance: LedgerApi = new LedgerApi();
	public static get(): LedgerApi { return LedgerApi._instance; }

	public static parseControlEntry(elem: any): FacilityControlEntry {
		return {
			...elem,
			ratio: (elem.defended || 1) / (elem.captured || 1),
			total: elem.captured + elem.defended
		};
	}

	public static async getLedger(options?: LedgerOptions): Promise<Loading<FacilityControlEntry[]>> {
		const param: URLSearchParams = new URLSearchParams();

		if (options) {
			if (options.worldID.length > 0) {
				for (const worldID of options.worldID) {
					param.append("worldID", worldID.toString());
				}
			}

			if (options.playerThreshold) {
				param.set("playerThreshold", options.playerThreshold.toString());
			}
		}

		return LedgerApi.get().readList(`/api/ledger/?${param.toString()}`, LedgerApi.parseControlEntry);
	}
}
