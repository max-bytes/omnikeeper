import { useQuery } from '@apollo/client';
import { queries } from '../graphql/queries'

export function useSelectedTime() {
    const { data } = useQuery(queries.SelectedTimeThreshold, {fetchPolicy: 'cache-only'});

    return data?.selectedTimeThreshold;
}
