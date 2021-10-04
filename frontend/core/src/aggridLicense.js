import {LicenseManager} from 'ag-grid-enterprise';
import env from "@beam-australia/react-env";

export default function setAGGridLicenseKey() {
    LicenseManager.setLicenseKey(env('AGGRID_LICENCE_KEY'));
}