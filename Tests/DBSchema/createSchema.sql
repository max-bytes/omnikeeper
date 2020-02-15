﻿
CREATE TYPE attributestate AS ENUM (
    'new',
    'changed',
    'removed',
    'renewed'
);
ALTER TYPE attributestate OWNER TO postgres;

CREATE TYPE relationstate AS ENUM (
    'new',
    'removed',
    'renewed'
);
ALTER TYPE relationstate OWNER TO postgres;




CREATE TABLE public.changeset
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    "timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT changeset_pkey PRIMARY KEY (id)
);

ALTER TABLE public.changeset
    OWNER to postgres;

    
CREATE TABLE public.relation
(
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    from_ci_id bigint NOT NULL,
    to_ci_id bigint NOT NULL,
    "predicate" text NOT NULL,
    activation_time timestamp with time zone NOT NULL,
    changeset_id bigint NOT NULL,
    layer_id bigint NOT NULL,
    state public.relationstate DEFAULT 'new'::public.relationstate NOT NULL,
    CONSTRAINT relation_pkey PRIMARY KEY (id)
);

ALTER TABLE public.relation
    OWNER to postgres;
--
-- TOC entry 202 (class 1259 OID 16394)
-- Name: ci; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ci (
    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    identity text NOT NULL,
    CONSTRAINT ci_pkey PRIMARY KEY (id)
);

ALTER TABLE public.ci OWNER TO postgres;



--
-- TOC entry 203 (class 1259 OID 16402)
-- Name: attribute; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.attribute (
    id bigint NOT NULL,
    name text NOT NULL,
    ci_id bigint NOT NULL,
    type character varying NOT NULL,
    value text NOT NULL,
    activation_time timestamp with time zone NOT NULL,
    layer_id bigint NOT NULL,
    state public.attributestate DEFAULT 'new'::public.attributestate NOT NULL,
    changeset_id bigint NOT NULL
);


ALTER TABLE public.attribute OWNER TO postgres;

--
-- TOC entry 206 (class 1259 OID 16430)
-- Name: attribute_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.attribute ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.attribute_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 204 (class 1259 OID 16415)
-- Name: layer; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.layer (
    id bigint NOT NULL,
    name character varying NOT NULL
);

ALTER TABLE public.layer
    ADD CONSTRAINT u_name UNIQUE ("name");

ALTER TABLE public.layer OWNER TO postgres;

--
-- TOC entry 207 (class 1259 OID 16432)
-- Name: layer_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.layer ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.layer_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


ALTER TABLE public.ci
    ADD CONSTRAINT u_identity UNIQUE ("identity");


--
-- TOC entry 2709 (class 2606 OID 16409)
-- Name: attribute attribute_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attribute
    ADD CONSTRAINT attribute_pkey PRIMARY KEY (id);


--
-- TOC entry 2711 (class 2606 OID 16422)
-- Name: layer layer_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layer
    ADD CONSTRAINT layer_pkey PRIMARY KEY (id);


--
-- TOC entry 2712 (class 2606 OID 16410)
-- Name: attribute f_anchor; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attribute
    ADD CONSTRAINT f_anchor FOREIGN KEY (ci_id) REFERENCES public.ci(id) ON UPDATE RESTRICT ON DELETE RESTRICT NOT VALID;


ALTER TABLE public.attribute
    ADD CONSTRAINT f_changeset FOREIGN KEY (changeset_id) REFERENCES public.changeset (id) ON UPDATE RESTRICT ON DELETE RESTRICT NOT VALID;

--
-- TOC entry 2713 (class 2606 OID 16423)
-- Name: attribute f_layer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attribute
    ADD CONSTRAINT f_layer FOREIGN KEY (layer_id) REFERENCES public.layer(id) NOT VALID;

    
    ALTER TABLE ONLY public.relation
        ADD CONSTRAINT f_from_id FOREIGN KEY (from_ci_id) REFERENCES public.ci(id) ON UPDATE RESTRICT ON DELETE RESTRICT NOT VALID,
        ADD CONSTRAINT f_to_ci FOREIGN KEY (to_ci_id) REFERENCES public.ci(id) ON UPDATE RESTRICT ON DELETE RESTRICT NOT VALID,
        ADD CONSTRAINT f_changeset FOREIGN KEY (changeset_id) REFERENCES public.changeset (id) ON UPDATE RESTRICT ON DELETE RESTRICT NOT VALID,
        ADD CONSTRAINT f_layer FOREIGN KEY (layer_id) REFERENCES public.layer(id) NOT VALID;

CREATE INDEX
    ON public.attribute USING btree
    (ci_id ASC NULLS LAST);


CREATE INDEX
    ON public.relation USING btree
    (from_ci_id ASC NULLS LAST);
CREATE INDEX
    ON public.relation USING btree
    (to_ci_id ASC NULLS LAST);