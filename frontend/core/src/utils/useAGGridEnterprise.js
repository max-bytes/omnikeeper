import { useState, useEffect } from "react";
import env from "@beam-australia/react-env";

/*
At the moment "ag-grid-enterprise" has been imported, enterprise features are supported.
However, if the validation of the license fails, the grid will show a watermark for 5 seconds (but enterprise features still work).
(See https://www.ag-grid.com/javascript-data-grid/licensing/#invalid-license for details.)

This leads to the following possibilities:
-   Valid License
        Enterprise features work.
-   Invalid License
        Enterprise features still work, but the grid will show a watermark for 5 seconds.
        This is e.g. useful, if a license has been expired. 
-   No License provided
        Enterprise features do not work.
*/

export function useAGGridEnterprise() {
    const [aGGridEnterpriseActive, setAGGridEnterpriseActive] = useState(null);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        try {
            setLoading(true);
            setAGGridEnterpriseActive(false);
            const license = env('AGGRID_LICENCE_KEY');
            if (license) {
                const licenseManager = require("ag-grid-enterprise").LicenseManager;    // Only 'require', if license provided, to ensure, watermark doesn't appear, when enterprise is not in use.
                licenseManager.setLicenseKey(license);
                setAGGridEnterpriseActive(true);
            }
            setLoading(false);
        } catch (e) {
            setError(e);
        }
    }, []);

    return { data: aGGridEnterpriseActive, loading: loading, error: error };
}