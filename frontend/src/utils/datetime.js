
import moment from 'moment'

export function formatTimestamp(ts) {
    return moment(ts).format('YYYY-MM-DD HH:mm:ss');
}