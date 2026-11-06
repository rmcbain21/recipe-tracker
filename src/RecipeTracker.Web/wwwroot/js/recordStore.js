// Thin IndexedDB interop for DexieRecordStore. All behaviour lives in the C# core;
// this module only puts, gets and lists records.
import Dexie from '../lib/dexie/dexie.min.mjs';

const db = new Dexie('recipe-tracker');

// One table for every record type, keyed by [table+id], so new record types
// need no schema change.
db.version(1).stores({
    records: '[table+id], table'
});

export async function save(table, record) {
    await db.records.put({ table, id: record.id, record });
}

// One transaction, so if any record fails to save, none are saved.
export async function saveAll(rows) {
    await db.transaction('rw', db.records, () =>
        db.records.bulkPut(rows.map(({ table, record }) => ({ table, id: record.id, record }))));
}

export async function load(table, id) {
    const row = await db.records.get([table, id]);
    return row ? row.record : null;
}

export async function list(table) {
    const rows = await db.records.where('table').equals(table).toArray();
    return rows.map(row => row.record);
}
