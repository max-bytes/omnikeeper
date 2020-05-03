import { useQuery } from '@apollo/client';
import { queries } from '../graphql/queries'

export function useSelectedTime() {
    const { error, data, loading } = useQuery(queries.SelectedTimeThreshold);

    return data?.selectedTimeThreshold;
}
